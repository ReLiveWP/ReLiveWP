using System.Text.Json;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// Graph's six recurrencePattern types line up with the six MS-OXCICAL templates, so unlike the RRULE
// providers it should almost never need the expansion fallback. These pin that claim down.
public class GraphCalendarProjectionTests
{
    private static readonly ExpansionWindow Window =
        new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), 500);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static GraphEvent Event(string json) => JsonSerializer.Deserialize<GraphEvent>(json, Json)!;

    private static ProjectedCalendar Project(GraphEvent master) =>
        GraphCalendarProjection.Project(master, Window);

    private static CalendarItem Single(GraphEvent master)
    {
        var projected = Project(master);

        Assert.Null(projected.ExpandedBecause);
        return Assert.Single(projected.Events).Calendar;
    }

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    private static string Recurring(string pattern, string range = """{ "type": "noEnd", "startDate": "2026-06-15" }""") => $$"""
    {
      "id": "evt1", "subject": "Series", "showAs": "busy",
      "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
      "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" },
      "recurrence": { "pattern": {{pattern}}, "range": {{range}} }
    }
    """;

    [Fact]
    public void A_plain_timed_event_carries_its_times_and_text()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "iCalUId": "040000008200E00074C5B7101A82E008", "subject": "Standup",
          "bodyPreview": "the usual", "showAs": "busy",
          "location": { "displayName": "Room 3" },
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """));

        Assert.Equal(Utc(2026, 6, 15, 14), item.StartTime.ToDateTime());
        Assert.Equal(Utc(2026, 6, 15, 15), item.EndTime.ToDateTime());
        Assert.Equal("Standup", item.Subject);
        Assert.Equal("Room 3", item.Location);
        Assert.Equal("the usual", item.Notes);
        Assert.Equal(2u, item.BusyStatus);
    }

    // graph writes the local wall time and names the zone separately, so 09:00 in London in June is
    // 08:00Z rather than 09:00Z
    [Fact]
    public void A_local_time_is_resolved_through_its_named_zone()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "showAs": "busy",
          "start": { "dateTime": "2026-06-15T09:00:00.0000000", "timeZone": "Europe/London" },
          "end":   { "dateTime": "2026-06-15T10:00:00.0000000", "timeZone": "Europe/London" }
        }
        """));

        Assert.Equal(Utc(2026, 6, 15, 8), item.StartTime.ToDateTime());
        Assert.True(item.HasTimezone);
    }

    [Fact]
    public void An_all_day_event_lands_on_utc_midnight_with_no_timezone()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "showAs": "free", "isAllDay": true,
          "start": { "dateTime": "2026-06-15T00:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-16T00:00:00.0000000", "timeZone": "UTC" }
        }
        """));

        Assert.True(item.AllDayEvent);
        Assert.Equal(Utc(2026, 6, 15), item.StartTime.ToDateTime());
        Assert.False(item.HasTimezone);
    }

    // the six patterns against the six EAS Type values, which is the whole reason graph needs no
    // RRULE parsing at all
    [Fact]
    public void Daily_maps_to_type_0()
    {
        var item = Single(Event(Recurring("""{ "type": "daily", "interval": 3 }""")));

        Assert.Equal(0u, item.RecurrenceType);
        Assert.Equal(3u, item.RecurrenceInterval);
    }

    [Fact]
    public void Weekly_maps_to_type_1_with_a_day_mask()
    {
        var item = Single(Event(Recurring(
            """{ "type": "weekly", "interval": 2, "daysOfWeek": ["monday","tuesday"], "firstDayOfWeek": "monday" }""")));

        Assert.Equal(1u, item.RecurrenceType);
        Assert.Equal(EasDayOfWeek.Monday | EasDayOfWeek.Tuesday, item.RecurrenceDayOfWeek);
        Assert.Equal(1u, item.RecurrenceFirstDayOfWeek);
    }

    [Fact]
    public void Absolute_monthly_maps_to_type_2()
    {
        var item = Single(Event(Recurring("""{ "type": "absoluteMonthly", "interval": 3, "dayOfMonth": 15 }""")));

        Assert.Equal(2u, item.RecurrenceType);
        Assert.Equal(15u, item.RecurrenceDayOfMonth);
    }

    [Fact]
    public void Relative_monthly_maps_to_type_3()
    {
        var item = Single(Event(Recurring(
            """{ "type": "relativeMonthly", "interval": 1, "daysOfWeek": ["thursday"], "index": "second" }""")));

        Assert.Equal(3u, item.RecurrenceType);
        Assert.Equal(EasDayOfWeek.Thursday, item.RecurrenceDayOfWeek);
        Assert.Equal(2u, item.RecurrenceWeekOfMonth);
    }

    [Fact]
    public void Absolute_yearly_maps_to_type_5()
    {
        var item = Single(Event(Recurring(
            """{ "type": "absoluteYearly", "interval": 1, "dayOfMonth": 15, "month": 3 }""")));

        Assert.Equal(5u, item.RecurrenceType);
        Assert.Equal(15u, item.RecurrenceDayOfMonth);
        Assert.Equal(3u, item.RecurrenceMonthOfYear);
    }

    [Fact]
    public void Relative_yearly_maps_to_type_6()
    {
        var item = Single(Event(Recurring(
            """{ "type": "relativeYearly", "interval": 1, "daysOfWeek": ["thursday"], "index": "last", "month": 11 }""")));

        Assert.Equal(6u, item.RecurrenceType);
        Assert.Equal(5u, item.RecurrenceWeekOfMonth);
        Assert.Equal(11u, item.RecurrenceMonthOfYear);
    }

    // the weekdays and weekend-days sets are the 62 and 65 masks, which is what makes "the first
    // working day of the month" representable at all
    [Fact]
    public void A_weekday_set_maps_to_the_62_mask()
    {
        var item = Single(Event(Recurring("""
        { "type": "relativeMonthly", "interval": 1,
          "daysOfWeek": ["monday","tuesday","wednesday","thursday","friday"], "index": "first" }
        """)));

        Assert.Equal(3u, item.RecurrenceType);
        Assert.Equal(62u, item.RecurrenceDayOfWeek);
        Assert.Equal(1u, item.RecurrenceWeekOfMonth);
    }

    // range endDate and numbered are exactly the Until-xor-Occurrences rule the validator enforces
    [Fact]
    public void A_numbered_range_becomes_occurrences()
    {
        var item = Single(Event(Recurring(
            """{ "type": "daily", "interval": 1 }""",
            """{ "type": "numbered", "startDate": "2026-06-15", "numberOfOccurrences": 10 }""")));

        Assert.Equal(10u, item.RecurrenceOccurrences);
        Assert.Null(item.RecurrenceUntil);
        Assert.True(item.HasRecurrenceOccurrences);
    }

    [Fact]
    public void An_end_date_range_becomes_until()
    {
        var item = Single(Event(Recurring(
            """{ "type": "daily", "interval": 1 }""",
            """{ "type": "endDate", "startDate": "2026-06-15", "endDate": "2026-12-31" }""")));

        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), item.RecurrenceUntil.ToDateTime());
        Assert.False(item.HasRecurrenceOccurrences);
    }

    [Fact]
    public void A_no_end_range_leaves_both_unset()
    {
        var item = Single(Event(Recurring("""{ "type": "daily", "interval": 1 }""")));

        Assert.False(item.HasRecurrenceOccurrences);
        Assert.Null(item.RecurrenceUntil);
    }

    // an arbitrary relative day set is not one of the three EAS masks, so it falls back
    [Fact]
    public void An_arbitrary_relative_day_set_expands()
    {
        var projected = Project(Event(Recurring(
            """{ "type": "relativeMonthly", "interval": 1, "daysOfWeek": ["monday","wednesday"], "index": "first" }""",
            """{ "type": "numbered", "startDate": "2026-06-15", "numberOfOccurrences": 4 }""")));

        Assert.NotNull(projected.ExpandedBecause);
        Assert.All(projected.Events, e => Assert.Equal(0u, e.Calendar.RecurrenceType));
        Assert.Equal(projected.Events.Count, projected.Events.Select(e => e.ExternalId).Distinct().Count());
    }

    // cancelledOccurrences arrives as master id and instance date joined by a dot
    [Fact]
    public void A_cancelled_occurrence_becomes_a_deleted_exception()
    {
        var master = Event(Recurring("""{ "type": "weekly", "interval": 1, "daysOfWeek": ["monday"] }"""));
        master.CancelledOccurrences = ["OID.evt1.20260622T140000Z"];

        var exception = Assert.Single(Single(master).Exceptions);

        Assert.True(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
    }

    // originalStart is the instance's slot in the series, not where it moved to
    [Fact]
    public void An_exception_occurrence_becomes_a_changed_exception()
    {
        var master = Event(Recurring("""{ "type": "weekly", "interval": 1, "daysOfWeek": ["monday"] }"""));
        master.ExceptionOccurrences =
        [
            Event("""
            {
              "id": "evt1_ex", "subject": "Moved", "showAs": "busy",
              "originalStart": "2026-06-22T14:00:00.0000000Z",
              "start": { "dateTime": "2026-06-22T16:00:00.0000000", "timeZone": "UTC" },
              "end":   { "dateTime": "2026-06-22T17:00:00.0000000", "timeZone": "UTC" }
            }
            """)
        ];

        var exception = Assert.Single(Single(master).Exceptions);

        Assert.False(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
        Assert.Equal(Utc(2026, 6, 22, 16), exception.StartTime.ToDateTime());
        Assert.Equal("Moved", exception.Subject);
    }

    [Theory]
    [InlineData("free", 0u)]
    [InlineData("tentative", 1u)]
    [InlineData("busy", 2u)]
    [InlineData("oof", 3u)]
    [InlineData("workingElsewhere", 4u)]
    public void ShowAs_becomes_busy_status(string value, uint expected)
        => Assert.Equal(expected, Single(Event($$"""
        {
          "id": "evt1", "showAs": "{{value}}",
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """)).BusyStatus);

    [Theory]
    [InlineData("normal", 0u)]
    [InlineData("personal", 1u)]
    [InlineData("private", 2u)]
    [InlineData("confidential", 3u)]
    public void Sensitivity_comes_across(string value, uint expected)
        => Assert.Equal(expected, Single(Event($$"""
        {
          "id": "evt1", "showAs": "busy", "sensitivity": "{{value}}",
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """)).Sensitivity);

    [Fact]
    public void Attendees_carry_their_response_and_type()
    {
        var item = Single(Event("""
        {
          "id": "evt1", "showAs": "busy",
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" },
          "organizer": { "emailAddress": { "name": "Ada Lovelace", "address": "ada@example.com" } },
          "attendees": [
            { "type": "required", "emailAddress": { "name": "Alan Turing", "address": "alan@example.com" },
              "status": { "response": "accepted" } },
            { "type": "optional", "emailAddress": { "address": "grace@example.com" },
              "status": { "response": "declined" } },
            { "type": "resource", "emailAddress": { "address": "room@example.com" },
              "status": { "response": "notResponded" } }
          ]
        }
        """));

        Assert.Equal("Ada Lovelace", item.OrganizerName);
        Assert.Equal("ada@example.com", item.OrganizerEmail);
        Assert.Equal([3u, 4u, 5u], item.Attendees.Select(a => a.AttendeeStatus));
        Assert.Equal([1u, 2u, 3u], item.Attendees.Select(a => a.AttendeeType));
        Assert.Equal(3u, item.MeetingStatus);
    }

    [Fact]
    public void A_reminder_comes_across_only_when_it_is_on()
    {
        var on = Single(Event("""
        {
          "id": "evt1", "showAs": "busy", "isReminderOn": true, "reminderMinutesBeforeStart": 15,
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """));

        var off = Single(Event("""
        {
          "id": "evt1", "showAs": "busy", "isReminderOn": false, "reminderMinutesBeforeStart": 15,
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """));

        Assert.Equal(15u, on.Reminder);
        Assert.False(off.HasReminder);
    }

    // the per-event etag is what makes a full list on every poll cheap: unchanged events never write
    [Fact]
    public void The_odata_etag_is_carried_as_the_item_etag()
    {
        var projected = Project(Event("""
        {
          "id": "evt1", "showAs": "busy", "@odata.etag": "W/\"abc123\"",
          "start": { "dateTime": "2026-06-15T14:00:00.0000000", "timeZone": "UTC" },
          "end":   { "dateTime": "2026-06-15T15:00:00.0000000", "timeZone": "UTC" }
        }
        """));

        Assert.Equal("W/\"abc123\"", Assert.Single(projected.Events).Etag);
    }
}
