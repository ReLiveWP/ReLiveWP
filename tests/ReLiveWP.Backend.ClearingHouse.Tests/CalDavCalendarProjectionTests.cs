using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// One .ics is a whole series: the VEVENT without a RECURRENCE-ID is the master and the rest override
// single instances. What comes out has to satisfy ItemValidationRules, which rejects rather than
// corrects most of what is checked here.
public class CalDavCalendarProjectionTests
{
    private static readonly ExpansionWindow Window =
        new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), 500);

    private static string Ics(params string[] body) => string.Join("\r\n",
        ["BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//test//EN", .. body, "END:VCALENDAR"]);

    private static string Event(params string[] lines) =>
        Ics(["BEGIN:VEVENT", "UID:evt-1", "DTSTAMP:20260601T090000Z", .. lines, "END:VEVENT"]);

    private static ProjectedCalendar Project(string ics) =>
        ICalendarProjection.Project("cal/evt-1.ics", "etag-1", ics, Window);

    private static CalendarItem Single(string ics)
    {
        var projected = Project(ics);

        Assert.Null(projected.ExpandedBecause);
        return Assert.Single(projected.Events).Calendar;
    }

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void A_plain_timed_event_carries_its_times_and_text()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T150000Z",
            "SUMMARY:Standup",
            "LOCATION:Room 3",
            "DESCRIPTION:the usual"));

        Assert.Equal(Utc(2026, 6, 15, 14), item.StartTime.ToDateTime());
        Assert.Equal(Utc(2026, 6, 15, 15), item.EndTime.ToDateTime());
        Assert.Equal("Standup", item.Subject);
        Assert.Equal("Room 3", item.Location);
        Assert.Equal("the usual", item.Notes);
        Assert.Equal("evt-1", item.Uid);
        Assert.False(item.AllDayEvent);
    }

    // an all-day event is a date, not an instant. the validator truncates to UTC midnight anyway,
    // but relying on that logs a correction on every single run.
    [Fact]
    public void An_all_day_event_lands_on_utc_midnight_with_no_timezone()
    {
        var item = Single(Event(
            "DTSTART;VALUE=DATE:20260615",
            "DTEND;VALUE=DATE:20260616",
            "SUMMARY:Conference"));

        Assert.True(item.AllDayEvent);
        Assert.Equal(Utc(2026, 6, 15), item.StartTime.ToDateTime());
        Assert.Equal(Utc(2026, 6, 16), item.EndTime.ToDateTime());
        Assert.False(item.HasTimezone);
    }

    [Fact]
    public void A_zoned_event_carries_a_timezone_blob()
    {
        var item = Single(Event(
            "DTSTART;TZID=Europe/London:20260615T090000",
            "DTEND;TZID=Europe/London:20260615T100000",
            "SUMMARY:Zoned"));

        Assert.True(item.HasTimezone);
        Assert.Equal(EasTimeZone.Size, Convert.FromBase64String(item.Timezone).Length);
    }

    [Fact]
    public void A_weekly_series_keeps_its_recurrence()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T150000Z",
            "SUMMARY:Weekly",
            "RRULE:FREQ=WEEKLY;BYDAY=MO"));

        Assert.Equal(1u, item.RecurrenceType);
        Assert.Equal(EasDayOfWeek.Monday, item.RecurrenceDayOfWeek);
    }

    // EXDATE is how a single instance is dropped from a series
    [Fact]
    public void An_exdate_becomes_a_deleted_exception()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T150000Z",
            "RRULE:FREQ=WEEKLY;BYDAY=MO",
            "EXDATE:20260622T140000Z"));

        var exception = Assert.Single(item.Exceptions);

        Assert.True(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
    }

    // a second VEVENT sharing the UID and carrying RECURRENCE-ID is a changed instance, and the key
    // is the instance's *original* start, not its new one
    [Fact]
    public void A_recurrence_id_override_becomes_a_changed_exception()
    {
        var item = Single(Ics(
            "BEGIN:VEVENT", "UID:evt-1", "DTSTAMP:20260601T090000Z",
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z",
            "SUMMARY:Weekly", "RRULE:FREQ=WEEKLY;BYDAY=MO", "END:VEVENT",
            "BEGIN:VEVENT", "UID:evt-1", "DTSTAMP:20260601T090000Z",
            "RECURRENCE-ID:20260622T140000Z",
            "DTSTART:20260622T160000Z", "DTEND:20260622T170000Z",
            "SUMMARY:Moved", "END:VEVENT"));

        var exception = Assert.Single(item.Exceptions);

        Assert.False(exception.Deleted);
        Assert.Equal("20260622T140000Z", exception.ExceptionStartTime);
        Assert.Equal(Utc(2026, 6, 22, 16), exception.StartTime.ToDateTime());
        Assert.Equal("Moved", exception.Subject);
    }

    // a rule outside the MS-OXCICAL templates is expanded instead of losing the tail of the series
    [Fact]
    public void An_unrepresentable_rule_expands_into_separate_items()
    {
        var projected = Project(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T150000Z",
            "SUMMARY:Odd",
            "RRULE:FREQ=MONTHLY;BYDAY=MO,WE;BYSETPOS=1;COUNT=4"));

        Assert.NotNull(projected.ExpandedBecause);
        Assert.Equal(4, projected.Events.Count);

        Assert.All(projected.Events, e => Assert.Equal(0u, e.Calendar.RecurrenceType));
        Assert.All(projected.Events, e => Assert.False(e.Calendar.HasRecurrenceInterval));

        // each instance needs its own origin id or they collapse onto one row
        Assert.Equal(projected.Events.Count, projected.Events.Select(e => e.ExternalId).Distinct().Count());
        Assert.All(projected.Events, e => Assert.StartsWith("cal/evt-1.ics#", e.ExternalId));
    }

    [Fact]
    public void An_expanded_instance_keeps_the_master_duration()
    {
        var projected = Project(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T153000Z",
            "RRULE:FREQ=MONTHLY;BYDAY=MO,WE;BYSETPOS=1;COUNT=2"));

        Assert.All(projected.Events, e => Assert.Equal(
            TimeSpan.FromMinutes(90),
            e.Calendar.EndTime.ToDateTime() - e.Calendar.StartTime.ToDateTime()));
    }

    // RANGE=THISANDFUTURE moves every later instance, which a per-instance EAS exception cannot say
    [Fact]
    public void A_this_and_future_override_forces_expansion()
    {
        var projected = Project(Ics(
            "BEGIN:VEVENT", "UID:evt-1", "DTSTAMP:20260601T090000Z",
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z",
            "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=5", "END:VEVENT",
            "BEGIN:VEVENT", "UID:evt-1", "DTSTAMP:20260601T090000Z",
            "RECURRENCE-ID;RANGE=THISANDFUTURE:20260622T140000Z",
            "DTSTART:20260622T160000Z", "DTEND:20260622T170000Z", "END:VEVENT"));

        Assert.Contains("THISANDFUTURE", projected.ExpandedBecause);
        Assert.True(projected.Events.Count > 1);
    }

    [Fact]
    public void Attendees_and_the_organiser_come_across()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z",
            "DTEND:20260615T150000Z",
            "ORGANIZER;CN=Ada Lovelace:mailto:ada@example.com",
            "ATTENDEE;CN=Alan Turing:mailto:alan@example.com",
            "ATTENDEE:mailto:grace@example.com"));

        Assert.Equal("Ada Lovelace", item.OrganizerName);
        Assert.Equal("ada@example.com", item.OrganizerEmail);
        Assert.Equal(["alan@example.com", "grace@example.com"], item.Attendees.Select(a => a.Email));
        Assert.Equal("Alan Turing", item.Attendees[0].Name);
    }

    // MS-ASCAL 2.2.2.28: 0 means no attendees at all, so anything with them has to say meeting
    [Fact]
    public void Meeting_status_tracks_whether_there_are_attendees()
    {
        var alone = Single(Event("DTSTART:20260615T140000Z", "DTEND:20260615T150000Z"));
        var withOthers = Single(Event(
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z", "ATTENDEE:mailto:alan@example.com"));

        Assert.Equal(0u, alone.MeetingStatus);
        Assert.Equal(3u, withOthers.MeetingStatus);
    }

    [Theory]
    [InlineData("TRANSP:TRANSPARENT", 0u)]
    [InlineData("TRANSP:OPAQUE", 2u)]
    public void Transparency_becomes_busy_status(string line, uint expected)
        => Assert.Equal(expected,
            Single(Event("DTSTART:20260615T140000Z", "DTEND:20260615T150000Z", line)).BusyStatus);

    [Theory]
    [InlineData("CLASS:PUBLIC", 0u)]
    [InlineData("CLASS:PRIVATE", 2u)]
    [InlineData("CLASS:CONFIDENTIAL", 3u)]
    public void Class_becomes_sensitivity(string line, uint expected)
        => Assert.Equal(expected,
            Single(Event("DTSTART:20260615T140000Z", "DTEND:20260615T150000Z", line)).Sensitivity);

    [Fact]
    public void Categories_come_across()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z", "CATEGORIES:Work,Urgent"));

        Assert.Equal(["Work", "Urgent"], item.Categories.Select(c => c.Category));
    }

    // a VALARM trigger is a negative offset from the start; EAS wants positive minutes before
    [Fact]
    public void An_alarm_becomes_a_reminder_in_minutes()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z",
            "BEGIN:VALARM", "ACTION:DISPLAY", "TRIGGER:-PT15M", "DESCRIPTION:x", "END:VALARM"));

        Assert.Equal(15u, item.Reminder);
    }

    // iCloud writes all-day reminders as an offset into the day: PT9H is 9am on the day itself and
    // -PT15H is 9am the day before. Both blew up when the offset was anchored against a date-only
    // start, which took the whole event with them.
    [Fact]
    public void An_all_day_reminder_the_day_before_survives()
    {
        var item = Single(Event(
            "DTSTART;VALUE=DATE:20260615", "DTEND;VALUE=DATE:20260616", "SUMMARY:Birthday",
            "BEGIN:VALARM", "ACTION:DISPLAY", "TRIGGER:-PT15H", "DESCRIPTION:x", "END:VALARM"));

        Assert.Equal(900u, item.Reminder);
    }

    // a reminder after the start has no EAS form, so it drops rather than taking the event with it
    [Fact]
    public void An_all_day_reminder_on_the_day_is_dropped_without_losing_the_event()
    {
        var item = Single(Event(
            "DTSTART;VALUE=DATE:20260615", "DTEND;VALUE=DATE:20260616", "SUMMARY:Birthday",
            "BEGIN:VALARM", "ACTION:DISPLAY", "TRIGGER:PT9H", "DESCRIPTION:x", "END:VALARM"));

        Assert.False(item.HasReminder);
        Assert.Equal("Birthday", item.Subject);
    }

    [Fact]
    public void A_reminder_in_days_before_becomes_minutes()
    {
        var item = Single(Event(
            "DTSTART:20260615T140000Z", "DTEND:20260615T150000Z",
            "BEGIN:VALARM", "ACTION:DISPLAY", "TRIGGER:-P1D", "DESCRIPTION:x", "END:VALARM"));

        Assert.Equal(1440u, item.Reminder);
    }

    [Fact]
    public void An_ics_with_no_events_yields_nothing()
        => Assert.Empty(Project(Ics()).Events);
}
