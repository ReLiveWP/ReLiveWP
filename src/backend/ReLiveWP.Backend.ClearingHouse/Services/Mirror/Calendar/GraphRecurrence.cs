namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// Graph's six pattern types line up with the six MS-OXCICAL templates, which is no accident, so the
// only work here is restating them as the spec the mapper already takes.
public static class GraphRecurrence
{
    public static RecurrenceSpec? ToSpec(GraphPatternedRecurrence recurrence)
    {
        if (recurrence.Pattern is not { } pattern) return null;

        var frequency = Frequency(pattern.Type);
        if (frequency is null) return null;

        var byDay = (pattern.DaysOfWeek ?? [])
            .Select(Weekday)
            .Where(d => d is not null)
            .Select(d => new WeekdayOrdinal(d!.Value, null))
            .ToList();

        var relative = pattern.Type is "relativeMonthly" or "relativeYearly";

        return new RecurrenceSpec
        {
            Frequency = frequency.Value,
            Interval = pattern.Interval > 0 ? pattern.Interval : 1,
            ByDay = byDay,
            ByMonthDay = pattern.DayOfMonth > 0 ? [pattern.DayOfMonth] : [],
            ByMonth = pattern.Month > 0 ? [pattern.Month] : [],
            BySetPosition = relative ? [Index(pattern.Index)] : [],
            Count = recurrence.Range?.Type == "numbered" && recurrence.Range.NumberOfOccurrences > 0
                ? recurrence.Range.NumberOfOccurrences
                : null,
            Until = recurrence.Range?.Type == "endDate" && EasCompactTime.TryParse(recurrence.Range.EndDate, out var end)
                ? end
                : null,
            WeekStart = Weekday(pattern.FirstDayOfWeek),
        };
    }

    private static RecurrenceFrequency? Frequency(string? type) => type switch
    {
        "daily" => RecurrenceFrequency.Daily,
        "weekly" => RecurrenceFrequency.Weekly,
        "absoluteMonthly" or "relativeMonthly" => RecurrenceFrequency.Monthly,
        "absoluteYearly" or "relativeYearly" => RecurrenceFrequency.Yearly,
        _ => null,
    };

    // weekIndex maps straight onto EAS WeekOfMonth, last included. the default is first.
    private static int Index(string? index) => index switch
    {
        "second" => 2,
        "third" => 3,
        "fourth" => 4,
        "last" => -1,
        _ => 1,
    };

    private static DayOfWeek? Weekday(string? day) => day switch
    {
        "sunday" => DayOfWeek.Sunday,
        "monday" => DayOfWeek.Monday,
        "tuesday" => DayOfWeek.Tuesday,
        "wednesday" => DayOfWeek.Wednesday,
        "thursday" => DayOfWeek.Thursday,
        "friday" => DayOfWeek.Friday,
        "saturday" => DayOfWeek.Saturday,
        _ => null,
    };
}
