using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// Google returns a series as a master carrying recurrence[] plus a separate event per changed or
// cancelled instance, each pointing back with recurringEventId. They arrive flat, so grouping is
// this file's job, where CalDAV got it bundled into one .ics.
public static class GoogleCalendarProjection
{
    private const string Cancelled = "cancelled";

    public static ProjectedCalendar Project(
        GoogleEvent master, IReadOnlyList<GoogleEvent> overrides, ExpansionWindow window)
    {
        var zone = TimeZoneFor(master);
        var item = ToCalendarItem(master, zone);

        var rules = master.Recurrence ?? [];
        if (rules.Count == 0)
            return new ProjectedCalendar([new(master.Id!, item)], null);

        var rrules = rules.Where(r => r.StartsWith("RRULE", StringComparison.OrdinalIgnoreCase)).ToList();

        if (rrules.Count != 1)
            return ExpandEntries(master, overrides, zone, window,
                rrules.Count == 0 ? "the series has no RRULE" : "the series carries more than one RRULE");

        // RDATE adds one-off dates to a series, which EAS has no room for
        if (rules.Any(r => r.StartsWith("RDATE", StringComparison.OrdinalIgnoreCase)))
            return ExpandEntries(master, overrides, zone, window, "the series carries an RDATE");

        var mapping = RecurrenceMapper.Map(RRuleParser.Parse(rrules[0]), Utc(master.Start));

        if (mapping.Recurrence is not { } recurrence)
            return ExpandEntries(master, overrides, zone, window, mapping.Reason!);

        recurrence.ApplyTo(item);

        foreach (var line in rules.Where(r => r.StartsWith("EXDATE", StringComparison.OrdinalIgnoreCase)))
            foreach (var excluded in ExDates(line))
                item.Exceptions.Add(new CalendarException { Deleted = true, ExceptionStartTime = excluded });

        foreach (var changed in overrides)
            item.Exceptions.Add(ToException(changed, zone));

        return new ProjectedCalendar([new(master.Id!, item)], null);
    }

    private static ProjectedCalendar ExpandEntries(
        GoogleEvent master, IReadOnlyList<GoogleEvent> overrides, TimeZoneInfo? zone,
        ExpansionWindow window, string reason)
    {
        var start = Utc(master.Start);
        var end = master.EndTimeUnspecified ? start : Utc(master.End);
        var duration = end > start ? end - start : TimeSpan.Zero;
        var occurrences = RecurrenceExpander.Starts(start, master.Recurrence ?? [], window);

        var changed = overrides
            .Where(o => o.OriginalStartTime is not null)
            .GroupBy(o => EasCompactTime.From(Utc(o.OriginalStartTime)), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

        var events = new List<RemoteCalendarEvent>(occurrences.Count);

        foreach (var occurrence in occurrences)
        {
            var slot = EasCompactTime.From(occurrence);
            var id = $"{master.Id}{MirrorPlanner.InstanceSeparator}{slot}";

            if (changed.TryGetValue(slot, out var moved))
            {
                if (IsCancelled(moved)) continue;

                events.Add(new(id, ToCalendarItem(moved, TimeZoneFor(moved) ?? zone)));
                continue;
            }

            var item = ToCalendarItem(master, zone);
            item.StartTime = Timestamp.FromDateTime(occurrence);
            item.EndTime = Timestamp.FromDateTime(occurrence + duration);

            events.Add(new(id, item));
        }

        return new ProjectedCalendar(events, reason);
    }

    private static CalendarItem ToCalendarItem(GoogleEvent source, TimeZoneInfo? zone)
    {
        var allDay = source.Start?.Date is { Length: > 0 };
        var start = Utc(source.Start);

        // endTimeUnspecified means the end carries no meaning, so it gets the start rather than 1970
        var end = source.EndTimeUnspecified ? start : Utc(source.End);

        var item = new CalendarItem
        {
            StartTime = Timestamp.FromDateTime(start),
            EndTime = Timestamp.FromDateTime(end < start ? start : end),
            AllDayEvent = allDay,
            BusyStatus = BusyStatus(source),
            Sensitivity = Sensitivity(source),
            MeetingStatus = source.Attendees is { Count: > 0 } ? 3u : 0u,
        };

        if (source.ICalUID is { Length: > 0 } uid) item.Uid = uid;
        if (source.Summary is { Length: > 0 } summary) item.Subject = summary;
        if (source.Location is { Length: > 0 } location) item.Location = location;
        if (source.HangoutLink is { Length: > 0 } link) item.OnlineMeetingConfLink = link;

        if (source.Description is { Length: > 0 } description)
        {
            item.Notes = description;
            item.NativeBodyType = 1;
        }

        if (Timestamped(source.Updated) is { } updated) item.DtStamp = Timestamp.FromDateTime(updated);

        if (source.Organizer?.DisplayName is { Length: > 0 } organiser) item.OrganizerName = organiser;
        if (source.Organizer?.Email is { Length: > 0 } organiserEmail) item.OrganizerEmail = organiserEmail;

        // an all-day event is a date, and a timezone would let the device shift it off that date
        if (!allDay && zone is not null) item.Timezone = EasTimeZone.ToBase64(zone, start);

        if (Reminder(source) is { } reminder) item.Reminder = reminder;

        foreach (var attendee in source.Attendees ?? [])
        {
            if (attendee.Email is not { Length: > 0 } email) continue;

            item.Attendees.Add(new CalendarAttendee
            {
                Email = email,
                Name = attendee.DisplayName is { Length: > 0 } name ? name : email,
                AttendeeStatus = AttendeeStatus(attendee),
                AttendeeType = AttendeeType(attendee),
            });
        }

        return item;
    }

    private static CalendarException ToException(GoogleEvent source, TimeZoneInfo? zone)
    {
        var original = EasCompactTime.From(Utc(source.OriginalStartTime));

        if (IsCancelled(source))
            return new CalendarException { Deleted = true, ExceptionStartTime = original };

        var item = ToCalendarItem(source, zone);

        var exception = new CalendarException
        {
            ExceptionStartTime = original,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            AllDayEvent = item.AllDayEvent,
            BusyStatus = item.BusyStatus,
            Sensitivity = item.Sensitivity,
            MeetingStatus = item.MeetingStatus,
        };

        if (item.HasSubject) exception.Subject = item.Subject;
        if (item.HasLocation) exception.Location = item.Location;
        if (item.HasNotes) exception.Notes = item.Notes;
        if (item.DtStamp is not null) exception.DtStamp = item.DtStamp;

        return exception;
    }

    public static bool IsCancelled(GoogleEvent source) =>
        string.Equals(source.Status, Cancelled, StringComparison.OrdinalIgnoreCase);

    private static uint Sensitivity(GoogleEvent source) => source.Visibility?.ToLowerInvariant() switch
    {
        "private" => 2u,
        "confidential" => 3u,
        _ => 0u,
    };

    // eventType outOfOffice and a tentative status both say more than transparency does
    private static uint BusyStatus(GoogleEvent source)
    {
        if (string.Equals(source.EventType, "outOfOffice", StringComparison.OrdinalIgnoreCase)) return 3u;
        if (string.Equals(source.Status, "tentative", StringComparison.OrdinalIgnoreCase)) return 1u;

        return string.Equals(source.Transparency, "transparent", StringComparison.OrdinalIgnoreCase) ? 0u : 2u;
    }

    private static uint AttendeeStatus(GoogleEventPerson attendee) => attendee.ResponseStatus?.ToLowerInvariant() switch
    {
        "accepted" => 3u,
        "declined" => 4u,
        "tentative" => 2u,
        "needsaction" => 5u,
        _ => 0u,
    };

    private static uint AttendeeType(GoogleEventPerson attendee) =>
        attendee.Resource ? 3u : attendee.Optional ? 2u : 1u;

    // useDefault means whatever the calendar is set to, which the API never tells us. a popup is
    // what the phone can actually show, so it wins over an email reminder on the same event.
    private static uint? Reminder(GoogleEvent source) =>
        source.Reminders?.Overrides?
            .Where(r => r.Minutes >= 0)
            .OrderBy(r => string.Equals(r.Method, "popup", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(r => (uint?)r.Minutes)
            .FirstOrDefault();

    private static TimeZoneInfo? TimeZoneFor(GoogleEvent source)
    {
        if (source.Start?.TimeZone is not { Length: > 0 } id) return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static IEnumerable<string> ExDates(string line) =>
        RecurrenceExpander.ExtractDates(line).Select(EasCompactTime.From);

    private static DateTime? Timestamped(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;

    // date means an all-day event, which is a date rather than an instant and stays at UTC midnight
    private static DateTime Utc(GoogleEventDate? value)
    {
        if (value?.Date is { Length: > 0 } date &&
            DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return DateTime.SpecifyKind(day.Date, DateTimeKind.Utc);

        return Timestamped(value?.DateTime) ?? DateTime.UnixEpoch;
    }
}
