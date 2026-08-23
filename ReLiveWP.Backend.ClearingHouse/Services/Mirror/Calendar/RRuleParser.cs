using Ical.Net;
using Ical.Net.DataTypes;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// google and caldav both hand over raw RFC 5545 rules, so both land here on the way to the mapper.
public static class RRuleParser
{
    public static RecurrenceSpec Parse(string rrule)
    {
        var value = rrule.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase) ? rrule[6..] : rrule;

        return From(new RecurrencePattern(value), value);
    }

    // Ical.Net has already parsed the rule off a VEVENT, so the raw text is only still needed to
    // tell an absent WKST from an explicit MO.
    public static RecurrenceSpec From(RecurrencePattern pattern) => From(pattern, pattern.ToString());

    private static RecurrenceSpec From(RecurrencePattern pattern, string value)
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

            // Ical.Net fills FirstDayOfWeek in with the RFC default of MO, so an absent WKST is only
            // visible in the raw rule. MS treats absent as SU, and the mapper is what decides.
            WeekStart = value.Contains("WKST", StringComparison.OrdinalIgnoreCase)
                ? pattern.FirstDayOfWeek
                : null,
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
