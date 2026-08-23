using System.Text.RegularExpressions;
using Google.Protobuf.WellKnownTypes;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using ReLiveWP.Services.Grpc.Mailbox;
using ICalendar = Ical.Net.Calendar;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public sealed record ExpansionWindow(DateTime From, DateTime To, int MaxInstances);

public sealed record ProjectedCalendar(IReadOnlyList<RemoteCalendarEvent> Events, string? ExpandedBecause);

// One .ics resource holds a whole series: the VEVENT with no RECURRENCE-ID is the master and the
// rest are overrides of single instances.
public static class ICalendarProjection
{
    public static ProjectedCalendar Project(
        string externalId, string? etag, string ics, ExpansionWindow window)
    {
        var calendar = ICalendar.Load(ics);

        var events = calendar.Events.ToList();
        if (events.Count == 0) return new ProjectedCalendar([], null);

        var master = events.FirstOrDefault(e => e.RecurrenceId is null) ?? events[0];
        var overrides = events.Where(e => e.RecurrenceId is not null).ToList();

        var zone = TimeZoneFor(master);
        var item = ToCalendarItem(master, zone);

        if (master.RecurrenceRules.Count == 0)
            return new ProjectedCalendar([new(externalId, item, etag)], null);

        if (master.RecurrenceRules.Count > 1)
            return Expand(externalId, etag, master, zone, window, "the series carries more than one RRULE");

        var spec = RRuleParser.From(master.RecurrenceRules[0]);
        var mapping = RecurrenceMapper.Map(spec, Utc(master.Start));

        if (mapping.Recurrence is not { } recurrence)
            return Expand(externalId, etag, master, zone, window, mapping.Reason!);

        // RANGE=THISANDFUTURE moves every later instance too, which an EAS exception cannot say
        if (HasThisAndFuture(ics))
            return Expand(externalId, etag, master, zone, window,
                "an override carries RECURRENCE-ID;RANGE=THISANDFUTURE");

        recurrence.ApplyTo(item);

        foreach (var deleted in DeletedInstances(master))
            item.Exceptions.Add(new CalendarException { Deleted = true, ExceptionStartTime = deleted });

        foreach (var changed in overrides)
            item.Exceptions.Add(ToException(changed, zone));

        return new ProjectedCalendar([new(externalId, item, etag)], null);
    }

    // A rule EAS cannot carry becomes its own item per instance rather than losing the tail of the
    // series. Each keeps a derived id so the planner still tracks it against the same origin.
    private static ProjectedCalendar Expand(
        string externalId, string? etag, CalendarEvent master, TimeZoneInfo? zone,
        ExpansionWindow window, string reason)
    {
        var occurrences = RecurrenceExpander.Starts(master, window);

        var events = new List<RemoteCalendarEvent>(occurrences.Count);
        var duration = Utc(master.End) - Utc(master.Start);

        foreach (var start in occurrences)
        {
            var item = ToCalendarItem(master, zone);
            item.StartTime = Timestamp.FromDateTime(start);
            item.EndTime = Timestamp.FromDateTime(start + duration);

            events.Add(new($"{externalId}#{EasCompactTime.From(start)}", item, etag));
        }

        return new ProjectedCalendar(events, reason);
    }

    private static CalendarItem ToCalendarItem(CalendarEvent source, TimeZoneInfo? zone)
    {
        var start = Utc(source.Start);
        var allDay = source.Start is { HasTime: false };

        var item = new CalendarItem
        {
            StartTime = Timestamp.FromDateTime(allDay ? start.Date : start),
            EndTime = Timestamp.FromDateTime(allDay ? Utc(source.End).Date : Utc(source.End)),
            AllDayEvent = allDay,
            BusyStatus = BusyStatus(source),
            Sensitivity = Sensitivity(source),
            MeetingStatus = MeetingStatus(source),
        };

        if (source.Uid is { Length: > 0 } uid) item.Uid = uid;
        if (source.Summary is { Length: > 0 } summary) item.Subject = summary;
        if (source.Location is { Length: > 0 } location) item.Location = location;
        if (source.DtStamp is not null) item.DtStamp = Timestamp.FromDateTime(Utc(source.DtStamp));

        if (source.Description is { Length: > 0 } description)
        {
            item.Notes = description;
            item.NativeBodyType = 1;
        }

        if (source.Organizer?.CommonName is { Length: > 0 } organiser) item.OrganizerName = organiser;
        if (Address(source.Organizer?.Value) is { } organiserEmail) item.OrganizerEmail = organiserEmail;

        // an all-day event is a date, and a timezone would let the device shift it off that date
        if (!allDay && zone is not null) item.Timezone = EasTimeZone.ToBase64(zone, start);

        if (Reminder(source) is { } reminder) item.Reminder = reminder;

        foreach (var attendee in source.Attendees)
        {
            if (Address(attendee.Value) is not { } email) continue;

            item.Attendees.Add(new CalendarAttendee
            {
                Email = email,
                Name = attendee.CommonName is { Length: > 0 } name ? name : email,
            });
        }

        foreach (var category in source.Categories.Where(c => !string.IsNullOrWhiteSpace(c)))
            item.Categories.Add(new CalendarCategory { Category = category });

        return item;
    }

    private static CalendarException ToException(CalendarEvent source, TimeZoneInfo? zone)
    {
        var item = ToCalendarItem(source, zone);

        var exception = new CalendarException
        {
            ExceptionStartTime = EasCompactTime.From(Utc(source.RecurrenceId!)),
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            AllDayEvent = item.AllDayEvent,
            BusyStatus = item.BusyStatus,
            Sensitivity = item.Sensitivity,
            MeetingStatus = item.MeetingStatus,
            DtStamp = item.DtStamp,
        };

        if (item.HasSubject) exception.Subject = item.Subject;
        if (item.HasLocation) exception.Location = item.Location;
        if (item.HasNotes) exception.Notes = item.Notes;

        return exception;
    }

    private static IEnumerable<string> DeletedInstances(CalendarEvent master) =>
        master.ExceptionDates.GetAllDates().Select(d => EasCompactTime.From(d.AsUtc));

    // Ical.Net folds RECURRENCE-ID into a CalDateTime and drops RANGE on the way, so this reads the
    // property off the unfolded source instead
    private static readonly Regex ThisAndFuture = new(
        @"^RECURRENCE-ID[^:\r\n]*;RANGE=THISANDFUTURE",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool HasThisAndFuture(string ics) => ThisAndFuture.IsMatch(Unfold(ics));

    // RFC 5545 3.1: a CRLF followed by a space or tab continues the previous line
    private static string Unfold(string ics) => ics.Replace("\r\n ", "").Replace("\r\n\t", "");

    private static TimeZoneInfo? TimeZoneFor(CalendarEvent source)
    {
        if (source.Start?.TzId is not { Length: > 0 } id) return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static uint BusyStatus(CalendarEvent source) =>
        string.Equals(source.Transparency, "TRANSPARENT", StringComparison.OrdinalIgnoreCase) ? 0u : 2u;

    private static uint Sensitivity(CalendarEvent source) => source.Class?.ToUpperInvariant() switch
    {
        "PRIVATE" => 2u,
        "CONFIDENTIAL" => 3u,
        _ => 0u,
    };

    // 3 is "a meeting you did not organise". claiming 1 would offer organiser actions this mirror
    // cannot honour, and 0 is defined as having no attendees at all.
    private static uint MeetingStatus(CalendarEvent source) => source.Attendees.Count > 0 ? 3u : 0u;

    // anchoring against the event start throws on an all-day one, because Apple writes reminders on
    // those as an offset into the day (TRIGGER:PT9H is 9am, -PT15H is 9am the day before) and hours
    // cannot be added to a date-only value. EAS only wants a count of minutes, so no anchor is
    // needed. a positive offset is a reminder after the start, which EAS has no way to say.
    private static uint? Reminder(CalendarEvent source)
    {
        var trigger = source.Alarms
            .Select(a => a.Trigger?.Duration)
            .FirstOrDefault(d => d is not null)?
            .ToTimeSpanUnspecified();

        return trigger is { } duration && duration <= TimeSpan.Zero
            ? (uint)Math.Round(-duration.TotalMinutes)
            : null;
    }

    // a mailto Uri keeps the address in UserInfo and Host, so AbsolutePath comes back empty
    private static string? Address(Uri? value)
    {
        if (value is null) return null;

        var text = value.OriginalString;

        return text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            ? text[7..] is { Length: > 0 } address ? address : null
            : null;
    }

    private static DateTime Utc(CalDateTime? value) => value?.AsUtc ?? DateTime.UnixEpoch;
}
