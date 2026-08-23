using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// A series master carries its changed instances in exceptionOccurrences and its dropped ones in
// cancelledOccurrences, both of which only arrive on a GET of the master itself.
public static class GraphCalendarProjection
{
    public static ProjectedCalendar Project(GraphEvent master, ExpansionWindow window)
    {
        var zone = TimeZoneFor(master);
        var item = ToCalendarItem(master, zone);

        if (master.Recurrence is not { } recurrence)
            return new ProjectedCalendar([new(master.Id!, item, master.Etag)], null);

        if (GraphRecurrence.ToSpec(recurrence) is not { } spec)
            return Expand(master, zone, window, $"recurrence pattern '{recurrence.Pattern?.Type}' is not one we read");

        var mapping = RecurrenceMapper.Map(spec, Utc(master.Start));

        if (mapping.Recurrence is not { } mapped)
            return Expand(master, zone, window, mapping.Reason!);

        mapped.ApplyTo(item);

        foreach (var cancelled in master.CancelledOccurrences ?? [])
            if (OccurrenceStart(cancelled) is { } start)
                item.Exceptions.Add(new CalendarException { Deleted = true, ExceptionStartTime = start });

        foreach (var changed in master.ExceptionOccurrences ?? [])
            item.Exceptions.Add(ToException(changed, zone));

        return new ProjectedCalendar([new(master.Id!, item, master.Etag)], null);
    }

    private static ProjectedCalendar Expand(
        GraphEvent master, TimeZoneInfo? zone, ExpansionWindow window, string reason)
    {
        var start = Utc(master.Start);
        var duration = Utc(master.End) - start;
        var rrule = GraphRecurrence.ToSpec(master.Recurrence!) is { } spec ? RRule(spec) : null;

        var occurrences = rrule is null ? [] : RecurrenceExpander.Starts(start, [rrule], window);
        var events = new List<RemoteCalendarEvent>(occurrences.Count);

        var cancelled = (master.CancelledOccurrences ?? [])
            .Select(OccurrenceStart)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var occurrence in occurrences)
        {
            if (cancelled.Contains(EasCompactTime.From(occurrence))) continue;

            var item = ToCalendarItem(master, zone);
            item.StartTime = Timestamp.FromDateTime(occurrence);
            item.EndTime = Timestamp.FromDateTime(occurrence + (duration > TimeSpan.Zero ? duration : TimeSpan.Zero));

            events.Add(new($"{master.Id}#{EasCompactTime.From(occurrence)}", item, master.Etag));
        }

        return new ProjectedCalendar(events, reason);
    }

    // the expander speaks RFC 5545, so a pattern that failed the templates goes back out as one
    private static string RRule(RecurrenceSpec spec)
    {
        var parts = new List<string> { $"FREQ={spec.Frequency.ToString().ToUpperInvariant()}" };

        if (spec.Interval > 1) parts.Add($"INTERVAL={spec.Interval}");
        if (spec.ByDay.Count > 0) parts.Add($"BYDAY={string.Join(',', spec.ByDay.Select(d => Code(d.Day)))}");
        if (spec.ByMonthDay.Count > 0) parts.Add($"BYMONTHDAY={string.Join(',', spec.ByMonthDay)}");
        if (spec.ByMonth.Count > 0) parts.Add($"BYMONTH={string.Join(',', spec.ByMonth)}");
        if (spec.BySetPosition.Count > 0) parts.Add($"BYSETPOS={string.Join(',', spec.BySetPosition)}");
        if (spec.Count is { } count) parts.Add($"COUNT={count}");
        if (spec.Until is { } until) parts.Add($"UNTIL={EasCompactTime.From(until)}");

        return "RRULE:" + string.Join(';', parts);
    }

    private static string Code(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "SU",
        DayOfWeek.Monday => "MO",
        DayOfWeek.Tuesday => "TU",
        DayOfWeek.Wednesday => "WE",
        DayOfWeek.Thursday => "TH",
        DayOfWeek.Friday => "FR",
        _ => "SA",
    };

    private static CalendarItem ToCalendarItem(GraphEvent source, TimeZoneInfo? zone)
    {
        var start = Utc(source.Start);
        var end = Utc(source.End);

        var item = new CalendarItem
        {
            StartTime = Timestamp.FromDateTime(source.IsAllDay ? start.Date : start),
            EndTime = Timestamp.FromDateTime(source.IsAllDay ? end.Date : end),
            AllDayEvent = source.IsAllDay,
            BusyStatus = BusyStatus(source),
            Sensitivity = Sensitivity(source),
            MeetingStatus = source.Attendees is { Count: > 0 } ? 3u : 0u,
        };

        if (source.ICalUId is { Length: > 0 } uid) item.Uid = uid;
        if (source.Subject is { Length: > 0 } subject) item.Subject = subject;
        if (source.Location?.DisplayName is { Length: > 0 } location) item.Location = location;
        if (source.OnlineMeetingUrl is { Length: > 0 } meeting) item.OnlineMeetingConfLink = meeting;
        if (source.Organizer?.EmailAddress?.Name is { Length: > 0 } name) item.OrganizerName = name;
        if (source.Organizer?.EmailAddress?.Address is { Length: > 0 } address) item.OrganizerEmail = address;

        // the body is html far more often than not, and bodyPreview is the plain text of the same
        if (source.BodyPreview is { Length: > 0 } preview)
        {
            item.Notes = preview;
            item.NativeBodyType = 1;
        }

        if (Timestamped(source.LastModifiedDateTime) is { } modified)
            item.DtStamp = Timestamp.FromDateTime(modified);

        if (!source.IsAllDay && zone is not null) item.Timezone = EasTimeZone.ToBase64(zone, start);

        if (source.IsReminderOn && source.ReminderMinutesBeforeStart >= 0)
            item.Reminder = (uint)source.ReminderMinutesBeforeStart;

        foreach (var attendee in source.Attendees ?? [])
        {
            if (attendee.EmailAddress?.Address is not { Length: > 0 } email) continue;

            item.Attendees.Add(new CalendarAttendee
            {
                Email = email,
                Name = attendee.EmailAddress.Name is { Length: > 0 } who ? who : email,
                AttendeeStatus = AttendeeStatus(attendee),
                AttendeeType = AttendeeType(attendee),
            });
        }

        foreach (var category in source.Categories ?? [])
            if (!string.IsNullOrWhiteSpace(category))
                item.Categories.Add(new CalendarCategory { Category = category });

        return item;
    }

    private static CalendarException ToException(GraphEvent source, TimeZoneInfo? zone)
    {
        var original = Timestamped(source.OriginalStart) is { } when
            ? EasCompactTime.From(when)
            : EasCompactTime.From(Utc(source.Start));

        if (source.IsCancelled)
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

    // showAs carries what transparency and status say elsewhere, including out of office
    private static uint BusyStatus(GraphEvent source) => source.ShowAs?.ToLowerInvariant() switch
    {
        "free" => 0u,
        "tentative" => 1u,
        "oof" => 3u,
        "workingelsewhere" => 4u,
        _ => 2u,
    };

    private static uint Sensitivity(GraphEvent source) => source.Sensitivity?.ToLowerInvariant() switch
    {
        "personal" => 1u,
        "private" => 2u,
        "confidential" => 3u,
        _ => 0u,
    };

    private static uint AttendeeStatus(GraphAttendee attendee) => attendee.Status?.Response?.ToLowerInvariant() switch
    {
        "accepted" or "organizer" => 3u,
        "declined" => 4u,
        "tentativelyaccepted" => 2u,
        "notresponded" => 5u,
        _ => 0u,
    };

    private static uint AttendeeType(GraphAttendee attendee) => attendee.Type?.ToLowerInvariant() switch
    {
        "optional" => 2u,
        "resource" => 3u,
        _ => 1u,
    };

    private static TimeZoneInfo? TimeZoneFor(GraphEvent source)
    {
        var id = source.OriginalStartTimeZone is { Length: > 0 } original ? original : source.Start?.TimeZone;
        if (id is not { Length: > 0 }) return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }

    // a cancelled occurrence is reported as the master id and the instance date joined by a dot
    private static string? OccurrenceStart(string occurrenceId)
    {
        var tail = occurrenceId.Split('.')[^1];

        return EasCompactTime.TryParse(tail, out var parsed) ? EasCompactTime.From(parsed) : null;
    }

    private static DateTime? Timestamped(string? value) =>
        EasCompactTime.TryParse(value, out var parsed) ? parsed : null;

    // graph writes the local wall time and names the zone separately, so it has to be resolved
    private static DateTime Utc(GraphDateTimeTimeZone? value)
    {
        if (value?.DateTime is not { Length: > 0 } text) return DateTime.UnixEpoch;

        if (!System.DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return DateTime.UnixEpoch;

        if (local.Kind == DateTimeKind.Utc) return local;

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone ?? "UTC");
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }
    }
}
