using System.Text.Json;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// Google returns a series flat: a master carrying recurrence[], then one event per changed or
// cancelled instance pointing back with recurringEventId. CalDAV got that bundled into one .ics, so
// the grouping is the part that is genuinely new here.
public class GoogleCalendarProjectionTests
{
    private static readonly ExpansionWindow Window =
        new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), 500);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static GoogleEvent Event(string json) => JsonSerializer.Deserialize<GoogleEvent>(json, Json)!;

    private static ProjectedCalendar Project(GoogleEvent master, params GoogleEvent[] overrides) =>
        GoogleCalendarProjection.Project(master, overrides, Window);

    private static CalendarItem Single(GoogleEvent master, params GoogleEvent[] overrides)
    {
        var projected = Project(master, overrides);

        Assert.Null(projected.ExpandedBecause);
        return Assert.Single(projected.Events).Calendar;
    }

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    private const string Timed = """
    {
      "id": "evt1", "iCalUID": "evt1@google.com", "status": "confirmed",
      "summary": "Standup", "location": "Room 3", "description": "the usual",
      "updated": "2026-06-01T09:00:00.000Z",
      "start": { "dateTime": "2026-06-15T14:00:00Z" },
      "end":   { "dateTime": "2026-06-15T15:00:00Z" }
    }
    """;

    [Fact]
    public void A_plain_timed_event_carries_its_times_and_text()
    {
        var item = Single(Event(Timed));

        Assert.Equal(Utc(2026, 6, 15, 14), item.StartTime.ToDateTime());
        Assert.Equal(Utc(2026, 6, 15, 15), item.EndTime.ToDateTime());
        Assert.Equal("Standup", item.Subject);
        Assert.Equal("Room 3", item.Location);
        Assert.Equal("the usual", item.Notes);
        Assert.Equal("evt1@google.com", item.Uid);
        Assert.False(item.AllDayEvent);
    }

    // google says all-day with "date" rather than "dateTime", and it is a date not an instant
    [Fact]
    public void An_all_day_event_lands_on_utc_midnight_with_no_timezone()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed", "summary": "Conference",
          "start": { "date": "2026-06-15" }, "end": { "date": "2026-06-16" }
        }
        """));

        Assert.True(item.AllDayEvent);
        Assert.Equal(Utc(2026, 6, 15), item.StartTime.ToDateTime());
        Assert.Equal(Utc(2026, 6, 16), item.EndTime.ToDateTime());
        Assert.False(item.HasTimezone);
    }

    [Fact]
    public void A_zoned_event_carries_a_timezone_blob()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T09:00:00+01:00", "timeZone": "Europe/London" },
          "end":   { "dateTime": "2026-06-15T10:00:00+01:00", "timeZone": "Europe/London" }
        }
        """));

        Assert.True(item.HasTimezone);
        Assert.Equal(EasTimeZone.Size, Convert.FromBase64String(item.Timezone).Length);
    }

    [Fact]
    public void A_weekly_series_keeps_its_recurrence()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "recurrence": ["RRULE:FREQ=WEEKLY;BYDAY=MO"]
        }
        """));

        Assert.Equal(1u, item.RecurrenceType);
        Assert.Equal(EasDayOfWeek.Monday, item.RecurrenceDayOfWeek);
    }

    [Fact]
    public void An_exdate_becomes_a_deleted_exception()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "recurrence": ["RRULE:FREQ=WEEKLY;BYDAY=MO", "EXDATE;VALUE=DATE-TIME:20260622T140000Z"]
        }
        """));

        var exception = Assert.Single(item.Exceptions);

        Assert.True(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
    }

    private const string WeeklyMaster = """
    {
      "id": "evt1", "status": "confirmed", "summary": "Weekly",
      "start": { "dateTime": "2026-06-15T14:00:00Z" },
      "end":   { "dateTime": "2026-06-15T15:00:00Z" },
      "recurrence": ["RRULE:FREQ=WEEKLY;BYDAY=MO"]
    }
    """;

    // the key is originalStartTime, the instance's slot in the series, not where it moved to
    [Fact]
    public void A_moved_instance_becomes_a_changed_exception()
    {
        var item = Single(Event(WeeklyMaster), Event("""
        {
          "id": "evt1_20260622T140000Z", "status": "confirmed", "summary": "Moved",
          "recurringEventId": "evt1",
          "originalStartTime": { "dateTime": "2026-06-22T14:00:00Z" },
          "start": { "dateTime": "2026-06-22T16:00:00Z" },
          "end":   { "dateTime": "2026-06-22T17:00:00Z" }
        }
        """));

        var exception = Assert.Single(item.Exceptions);

        Assert.False(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
        Assert.Equal(Utc(2026, 6, 22, 16), exception.StartTime.ToDateTime());
        Assert.Equal("Moved", exception.Subject);
    }

    // a cancelled instance leaves a gap in the series; a cancelled master is a real delete, which is
    // the driver's job rather than the projection's
    [Fact]
    public void A_cancelled_instance_becomes_a_deleted_exception()
    {
        var item = Single(Event(WeeklyMaster), Event("""
        {
          "id": "evt1_20260629T140000Z", "status": "cancelled",
          "recurringEventId": "evt1",
          "originalStartTime": { "dateTime": "2026-06-29T14:00:00Z" }
        }
        """));

        var exception = Assert.Single(item.Exceptions);

        Assert.True(exception.Deleted);
        Assert.Equal("20260629T140000Z", exception.ExceptionStartTime);
    }

    [Fact]
    public void A_cancelled_master_is_reported_as_cancelled()
        => Assert.True(GoogleCalendarProjection.IsCancelled(
            Event("""{ "id": "evt1", "status": "cancelled" }""")));

    [Fact]
    public void An_unrepresentable_rule_expands_into_separate_items()
    {
        var projected = Project(Event("""
        {
          "id": "evt1", "status": "confirmed", "summary": "Odd",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:30:00Z" },
          "recurrence": ["RRULE:FREQ=MONTHLY;BYDAY=MO,WE;BYSETPOS=1;COUNT=4"]
        }
        """));

        Assert.NotNull(projected.ExpandedBecause);
        Assert.Equal(4, projected.Events.Count);
        Assert.All(projected.Events, e => Assert.Equal(0u, e.Calendar.RecurrenceType));
        Assert.Equal(4, projected.Events.Select(e => e.ExternalId).Distinct().Count());

        // the duration has to survive expansion or every instance collapses to an hour
        Assert.All(projected.Events, e => Assert.Equal(
            TimeSpan.FromMinutes(90),
            e.Calendar.EndTime.ToDateTime() - e.Calendar.StartTime.ToDateTime()));
    }

    // RDATE bolts extra one-off dates onto a series, which EAS recurrence has no room for
    [Fact]
    public void An_rdate_forces_expansion()
    {
        var projected = Project(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "recurrence": ["RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=3", "RDATE;VALUE=DATE-TIME:20260704T140000Z"]
        }
        """));

        Assert.Contains("RDATE", projected.ExpandedBecause);
    }

    [Fact]
    public void Attendees_and_the_organiser_come_across()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "organizer": { "email": "ada@example.com", "displayName": "Ada Lovelace" },
          "attendees": [
            { "email": "alan@example.com", "displayName": "Alan Turing" },
            { "email": "grace@example.com" }
          ]
        }
        """));

        Assert.Equal("Ada Lovelace", item.OrganizerName);
        Assert.Equal("ada@example.com", item.OrganizerEmail);
        Assert.Equal(["alan@example.com", "grace@example.com"], item.Attendees.Select(a => a.Email));
        Assert.Equal(3u, item.MeetingStatus);
    }

    [Theory]
    [InlineData("transparent", 0u)]
    [InlineData("opaque", 2u)]
    public void Transparency_becomes_busy_status(string value, uint expected)
        => Assert.Equal(expected, Single(Event($$"""
        {
          "id": "evt1", "status": "confirmed", "transparency": "{{value}}",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" }
        }
        """)).BusyStatus);

    [Theory]
    [InlineData("default", 0u)]
    [InlineData("private", 2u)]
    [InlineData("confidential", 3u)]
    public void Visibility_becomes_sensitivity(string value, uint expected)
        => Assert.Equal(expected, Single(Event($$"""
        {
          "id": "evt1", "status": "confirmed", "visibility": "{{value}}",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" }
        }
        """)).Sensitivity);

    [Fact]
    public void An_override_reminder_becomes_minutes_before()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "reminders": { "useDefault": false, "overrides": [ { "method": "popup", "minutes": 30 } ] }
        }
        """));

        Assert.Equal(30u, item.Reminder);
    }

    // a tentative event and an out-of-office one both say more than transparency does
    [Fact]
    public void A_tentative_event_is_busy_status_tentative()
        => Assert.Equal(1u, Single(Event("""
        {
          "id": "evt1", "status": "tentative",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" }
        }
        """)).BusyStatus);

    [Fact]
    public void An_out_of_office_event_is_busy_status_oof()
        => Assert.Equal(3u, Single(Event("""
        {
          "id": "evt1", "status": "confirmed", "eventType": "outOfOffice",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" }
        }
        """)).BusyStatus);

    // MS-ASCAL 2.2.2.4 and 2.2.2.5: 3 accept, 4 decline, 2 tentative, 5 not responded; 2 optional
    [Fact]
    public void Attendee_response_and_type_come_across()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "attendees": [
            { "email": "a@example.com", "responseStatus": "accepted" },
            { "email": "b@example.com", "responseStatus": "declined", "optional": true },
            { "email": "c@example.com", "responseStatus": "needsAction" },
            { "email": "room@example.com", "responseStatus": "tentative", "resource": true }
          ]
        }
        """));

        Assert.Equal([3u, 4u, 5u, 2u], item.Attendees.Select(a => a.AttendeeStatus));
        Assert.Equal([1u, 2u, 1u, 3u], item.Attendees.Select(a => a.AttendeeType));
    }

    // endTimeUnspecified means the end carries no meaning, and 1970 would be worse than the start
    [Fact]
    public void An_unspecified_end_falls_back_to_the_start()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed", "endTimeUnspecified": true,
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T14:00:00Z" }
        }
        """));

        Assert.Equal(item.StartTime, item.EndTime);
    }

    // a popup is what the phone can show, so it wins over an email reminder on the same event
    [Fact]
    public void A_popup_reminder_wins_over_an_email_one()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "reminders": { "useDefault": false, "overrides": [
            { "method": "email", "minutes": 1440 },
            { "method": "popup", "minutes": 10 } ] }
        }
        """));

        Assert.Equal(10u, item.Reminder);
    }

    // useDefault means "whatever the calendar is set to", and the API never says what that is
    [Fact]
    public void A_default_reminder_is_left_alone()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "status": "confirmed",
          "start": { "dateTime": "2026-06-15T14:00:00Z" },
          "end":   { "dateTime": "2026-06-15T15:00:00Z" },
          "reminders": { "useDefault": true }
        }
        """));

        Assert.False(item.HasReminder);
    }
}
