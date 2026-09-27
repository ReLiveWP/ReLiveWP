using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public static class RecurrenceExpander
{
    public static IReadOnlyList<DateTime> Starts(CalendarEvent master, ExpansionWindow window) =>
    [
        .. master
            .GetOccurrences(new CalDateTime(window.From, tzId: "UTC"))
            .TakeWhile(o => o.Period.StartTime.AsUtc < window.To)
            .Take(window.MaxInstances)
            .Select(o => o.Period.StartTime.AsUtc)
    ];

    public static IReadOnlyList<DateTime> Starts(
        DateTime startUtc, IEnumerable<string> recurrenceLines, ExpansionWindow window)
    {
        var master = new CalendarEvent
        {
            Start = new CalDateTime(startUtc, tzId: "UTC"),
            Duration = Ical.Net.DataTypes.Duration.FromHours(1),
        };

        foreach (var line in recurrenceLines)
        {
            if (line.StartsWith("RRULE", StringComparison.OrdinalIgnoreCase))
                master.RecurrenceRules.Add(new RecurrencePattern(GetLineValue(line)));
            else if (line.StartsWith("EXDATE", StringComparison.OrdinalIgnoreCase))
                foreach (var excluded in ExtractDates(line))
                    master.ExceptionDates.Add(new CalDateTime(excluded, tzId: "UTC"));
        }

        return master.RecurrenceRules.Count == 0 ? [] : Starts(master, window);
    }

    public static IReadOnlyList<DateTime> ExtractDates(string line)
    {
        var colon = line.IndexOf(':');
        var zone = colon < 0 ? null : TimeZoneOf(line[..colon]);

        var dates = new List<DateTime>();

        foreach (var raw in GetLineValue(line).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var part = raw.Trim();

            if (!EasCompactTime.TryParse(part, out var parsed)) continue;

            var floating = !part.EndsWith('Z') && part.Contains('T');

            dates.Add(zone is not null && floating ? ToUtc(parsed, zone) : parsed);
        }

        return dates;
    }

    private static string GetLineValue(string line)
    {
        var colon = line.IndexOf(':');
        return colon < 0 ? line : line[(colon + 1)..];
    }

    private static TimeZoneInfo? TimeZoneOf(string parameters)
    {
        foreach (var part in parameters.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!part.StartsWith("TZID=", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(part[5..].Trim().Trim('"'));
            }
            catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return null;
            }
        }

        return null;
    }

    // TryParse hands back the written wall clock labelled UTC, so it has to be re-anchored
    private static DateTime ToUtc(DateTime wall, TimeZoneInfo zone)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(wall, DateTimeKind.Unspecified), zone);
        }
        catch (ArgumentException)
        {
            return DateTime.SpecifyKind(wall, DateTimeKind.Utc);
        }
    }
}
