using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// A rule EAS cannot carry becomes one item per instance rather than losing the tail of the series.
// CalDAV already holds a parsed VEVENT; Google hands over RRULE text, so it gets a synthetic one and
// both go through the same evaluator.
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
            var value = line.Contains(':') ? line[(line.IndexOf(':') + 1)..] : line;

            if (line.StartsWith("RRULE", StringComparison.OrdinalIgnoreCase))
                master.RecurrenceRules.Add(new RecurrencePattern(value));
            else if (line.StartsWith("EXDATE", StringComparison.OrdinalIgnoreCase))
                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    if (EasCompactTime.TryParse(part.Trim(), out var excluded))
                        master.ExceptionDates.Add(new CalDateTime(excluded, tzId: "UTC"));
        }

        return master.RecurrenceRules.Count == 0 ? [] : Starts(master, window);
    }
}
