using Ical.Net;
using Ical.Net.DataTypes;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// google and caldav both hand over raw RFC 5545 rules, so both land here on the way to the mapper.
public static class RRuleParser
{
    public static RecurrenceSpec Parse(string rrule)
    {
        var value = rrule.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase) ? rrule[6..] : rrule;

        return From(new RecurrencePattern(value), value.Contains("WKST", StringComparison.OrdinalIgnoreCase));
    }

    public static RecurrenceSpec From(RecurrencePattern pattern, bool statesWeekStart) =>
        From(pattern, statesWeekStart ? pattern.FirstDayOfWeek : (DayOfWeek?)null);

    public static bool StatesWeekStart(string ics) =>
        Unfold(ics)
            .Split('\n')
            .Any(line => line.StartsWith("RRULE", StringComparison.OrdinalIgnoreCase)
                      && line.Contains("WKST", StringComparison.OrdinalIgnoreCase));

    private static string Unfold(string ics) => ics
        .Replace("\r\n ", "").Replace("\r\n\t", "")
        .Replace("\n ", "").Replace("\n\t", "");

    private static RecurrenceSpec From(RecurrencePattern pattern, DayOfWeek? weekStart)
    {
        return new RecurrenceSpec
        {
            Frequency = Frequency(pattern.Frequency),
            Interval = pattern.Interval,
            ByDay = [.. pattern.ByDay.Select(d => new WeekdayOrdinal(d.DayOfWeek, d.Offset == int.MinValue ? null : d.Offset))],
            ByMonthDay = [.. pattern.ByMonthDay],
            ByMonth = [.. pattern.ByMonth],
            BySetPosition = [.. pattern.BySetPosition],
            ByWeekNo = [.. pattern.ByWeekNo],
            ByYearDay = [.. pattern.ByYearDay],
            ByHour = [.. pattern.ByHour],
            ByMinute = [.. pattern.ByMinute],
            BySecond = [.. pattern.BySecond],
            Count = Occurrences(pattern),
            Until = Until(pattern),
            WeekStart = weekStart,
        };
    }

    private static RecurrenceFrequency Frequency(FrequencyType frequency) => frequency switch
    {
        FrequencyType.Secondly => RecurrenceFrequency.Secondly,
        FrequencyType.Minutely => RecurrenceFrequency.Minutely,
        FrequencyType.Hourly => RecurrenceFrequency.Hourly,
        FrequencyType.Daily => RecurrenceFrequency.Daily,
        FrequencyType.Weekly => RecurrenceFrequency.Weekly,
        FrequencyType.Monthly => RecurrenceFrequency.Monthly,
        _ => RecurrenceFrequency.Yearly,
    };

    private static int? Occurrences(RecurrencePattern pattern) =>
        pattern.Count is { } count && count > 0 ? count : null;

    private static DateTime? Until(RecurrencePattern pattern) =>
        pattern.Until is { } until ? until.AsUtc : null;
}
