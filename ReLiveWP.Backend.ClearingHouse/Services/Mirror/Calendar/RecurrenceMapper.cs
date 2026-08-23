namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public static class RecurrenceMapper
{
    private const uint Daily = 0;
    private const uint Weekly = 1;
    private const uint Monthly = 2;
    private const uint MonthlyNth = 3;
    private const uint Yearly = 5;
    private const uint YearlyNth = 6;

    private const int MaxInterval = 999;
    private const int MaxOccurrences = 999;

    private const uint LastWeekOfMonth = 5;

    public static RecurrenceMapping Map(RecurrenceSpec spec, DateTime start)
    {
        if (Reject(spec) is { } rejected)
            return RecurrenceMapping.Unrepresentable(rejected);

        if (Normalise(spec) is not { } normalised)
            return RecurrenceMapping.Unrepresentable(
                "BYDAY carries per-day ordinals that do not agree with each other or with BYSETPOS");

        return normalised.Frequency switch
        {
            RecurrenceFrequency.Daily => MapDaily(normalised, start),
            RecurrenceFrequency.Weekly => MapWeekly(normalised, start),
            RecurrenceFrequency.Monthly => MapMonthly(normalised, start),
            RecurrenceFrequency.Yearly => MapYearly(normalised, start),
            _ => RecurrenceMapping.Unrepresentable($"FREQ={normalised.Frequency} is below daily"),
        };
    }

    private static string? Reject(RecurrenceSpec spec) => spec switch
    {
        { Frequency: RecurrenceFrequency.Secondly or RecurrenceFrequency.Minutely or RecurrenceFrequency.Hourly } =>
            $"FREQ={spec.Frequency} is below daily",
        { BySecond.Count: > 0 } => "BYSECOND has no EAS equivalent",
        { ByHour.Count: > 1 } => "BYHOUR must not carry more than one value",
        { ByMinute.Count: > 1 } => "BYMINUTE must not carry more than one value",
        { ByWeekNo.Count: > 0 } => "BYWEEKNO has no EAS equivalent",
        { ByYearDay.Count: > 0 } => "BYYEARDAY has no EAS equivalent",
        { Count: not null, Until: not null } => "COUNT and UNTIL must not both be present",
        { Count: < 1 or > MaxOccurrences } => $"COUNT must be between 1 and {MaxOccurrences}",
        { Interval: < 1 or > MaxInterval } => $"INTERVAL must be between 1 and {MaxInterval}",
        { ByMonth.Count: > 1 } => "BYMONTH must not carry more than one value",
        { ByMonthDay.Count: > 1 } => "BYMONTHDAY must not carry more than one value",
        { BySetPosition.Count: > 1 } => "BYSETPOS must not carry more than one value",
        _ => null,
    };

    private static RecurrenceSpec? Normalise(RecurrenceSpec spec)
    {
        var ordinals = spec.ByDay.Where(d => d.Ordinal is not null).Select(d => d.Ordinal!.Value).Distinct().ToList();
        if (ordinals.Count == 0) return spec;
        if (ordinals.Count > 1) return null;

        var ordinal = ordinals[0];
        if (spec.BySetPosition.Count > 0 && spec.BySetPosition[0] != ordinal) return null;

        return spec with
        {
            ByDay = [.. spec.ByDay.Select(d => d with { Ordinal = null })],
            BySetPosition = [ordinal],
        };
    }

    private static RecurrenceMapping MapDaily(RecurrenceSpec spec, DateTime start)
    {
        if (spec.ByMonthDay.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=DAILY with BYMONTHDAY");
        if (spec.ByMonth.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=DAILY with BYMONTH");

        // FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR is every weekday, which EAS says as a weekly mask. it only
        // holds at INTERVAL=1: every-n-days-that-are-also-weekdays has no EAS shape.
        if (spec.ByDay.Count > 0)
        {
            return spec.Interval == 1
                ? MapWeekly(spec with { Frequency = RecurrenceFrequency.Weekly }, start)
                : RecurrenceMapping.Unrepresentable("FREQ=DAILY with BYDAY and INTERVAL greater than 1");
        }

        return RecurrenceMapping.Ok(new EasRecurrence
        {
            Type = Daily,
            Interval = (uint)spec.Interval,
            Occurrences = Occurrences(spec),
            Until = spec.Until,
        });
    }

    private static RecurrenceMapping MapWeekly(RecurrenceSpec spec, DateTime start)
    {
        if (spec.ByMonthDay.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=WEEKLY with BYMONTHDAY");
        if (spec.ByMonth.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=WEEKLY with BYMONTH");
        if (spec.BySetPosition.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=WEEKLY with BYSETPOS");

        var mask = spec.ByDay.Count > 0
            ? spec.ByDay.Aggregate(0u, (acc, d) => acc | EasDayOfWeek.Bit(d.Day))
            : EasDayOfWeek.Bit(start.DayOfWeek);

        return RecurrenceMapping.Ok(new EasRecurrence
        {
            Type = Weekly,
            Interval = (uint)spec.Interval,
            Occurrences = Occurrences(spec),
            Until = spec.Until,
            DayOfWeek = mask,
            FirstDayOfWeek = FirstDayOfWeek(spec),
        });
    }

    private static RecurrenceMapping MapMonthly(RecurrenceSpec spec, DateTime start)
    {
        if (spec.ByMonth.Count > 0) return RecurrenceMapping.Unrepresentable("FREQ=MONTHLY with BYMONTH");

        if (spec.ByDay.Count > 0)
        {
            if (spec.ByMonthDay.Count > 0)
                return RecurrenceMapping.Unrepresentable("FREQ=MONTHLY with both BYDAY and BYMONTHDAY");

            if (NthDayMask(spec.ByDay) is not { } mask)
                return RecurrenceMapping.Unrepresentable(
                    "an nth-weekday BYDAY set must be one day, SA+SU, MO..FR, or all seven");

            if (WeekOfMonth(spec) is not { } week)
                return RecurrenceMapping.Unrepresentable(
                    "an nth-weekday rule needs a BYSETPOS of -1 or 1 to 4");

            return RecurrenceMapping.Ok(new EasRecurrence
            {
                Type = MonthlyNth,
                Interval = (uint)spec.Interval,
                Occurrences = Occurrences(spec),
                Until = spec.Until,
                DayOfWeek = mask,
                WeekOfMonth = week,
            });
        }

        if (spec.BySetPosition.Count > 0)
            return RecurrenceMapping.Unrepresentable("FREQ=MONTHLY with BYSETPOS but no BYDAY");

        var day = spec.ByMonthDay.Count > 0 ? spec.ByMonthDay[0] : start.Day;

        // DayOfMonth is 1..31, so the last day of the month has to go through the nth form instead
        if (day == -1)
            return RecurrenceMapping.Ok(new EasRecurrence
            {
                Type = MonthlyNth,
                Interval = (uint)spec.Interval,
                Occurrences = Occurrences(spec),
                Until = spec.Until,
                DayOfWeek = EasDayOfWeek.LastDayOfMonth,
                WeekOfMonth = LastWeekOfMonth,
            });

        if (day is < 1 or > 31)
            return RecurrenceMapping.Unrepresentable($"BYMONTHDAY={day} is outside 1 to 31 and is not -1");

        return RecurrenceMapping.Ok(new EasRecurrence
        {
            Type = Monthly,
            Interval = (uint)spec.Interval,
            Occurrences = Occurrences(spec),
            Until = spec.Until,
            DayOfMonth = (uint)day,
        });
    }

    private static RecurrenceMapping MapYearly(RecurrenceSpec spec, DateTime start)
    {
        var month = (uint)(spec.ByMonth.Count > 0 ? spec.ByMonth[0] : start.Month);
        if (month is < 1 or > 12)
            return RecurrenceMapping.Unrepresentable($"BYMONTH={month} is outside 1 to 12");

        if (spec.ByDay.Count > 0)
        {
            if (spec.ByMonthDay.Count > 0)
                return RecurrenceMapping.Unrepresentable("FREQ=YEARLY with both BYDAY and BYMONTHDAY");

            if (NthDayMask(spec.ByDay) is not { } mask)
                return RecurrenceMapping.Unrepresentable(
                    "an nth-weekday BYDAY set must be one day, SA+SU, MO..FR, or all seven");

            if (WeekOfMonth(spec) is not { } week)
                return RecurrenceMapping.Unrepresentable(
                    "an nth-weekday rule needs a BYSETPOS of -1 or 1 to 4");

            return RecurrenceMapping.Ok(new EasRecurrence
            {
                Type = YearlyNth,
                Interval = (uint)spec.Interval,
                Occurrences = Occurrences(spec),
                Until = spec.Until,
                DayOfWeek = mask,
                WeekOfMonth = week,
                MonthOfYear = month,
            });
        }

        if (spec.BySetPosition.Count > 0)
            return RecurrenceMapping.Unrepresentable("FREQ=YEARLY with BYSETPOS but no BYDAY");

        var day = spec.ByMonthDay.Count > 0 ? spec.ByMonthDay[0] : start.Day;

        // same as monthly: 1..31 only, so the last day of a month becomes the nth form
        if (day == -1)
            return RecurrenceMapping.Ok(new EasRecurrence
            {
                Type = YearlyNth,
                Interval = (uint)spec.Interval,
                Occurrences = Occurrences(spec),
                Until = spec.Until,
                DayOfWeek = EasDayOfWeek.LastDayOfMonth,
                WeekOfMonth = LastWeekOfMonth,
                MonthOfYear = month,
            });

        if (day is < 1 or > 31)
            return RecurrenceMapping.Unrepresentable($"BYMONTHDAY={day} is outside 1 to 31 and is not -1");

        return RecurrenceMapping.Ok(new EasRecurrence
        {
            Type = Yearly,
            Interval = (uint)spec.Interval,
            Occurrences = Occurrences(spec),
            Until = spec.Until,
            DayOfMonth = (uint)day,
            MonthOfYear = month,
        });
    }

    private static uint? NthDayMask(IReadOnlyList<WeekdayOrdinal> byDay)
    {
        var days = byDay.Select(d => d.Day).ToHashSet();

        if (days.Count == 1) return EasDayOfWeek.Bit(days.Single());

        var mask = days.Aggregate(0u, (acc, d) => acc | EasDayOfWeek.Bit(d));

        return mask switch
        {
            EasDayOfWeek.WeekendDays => EasDayOfWeek.WeekendDays,
            EasDayOfWeek.Weekdays => EasDayOfWeek.Weekdays,
            EasDayOfWeek.LastDayOfMonth => EasDayOfWeek.LastDayOfMonth,
            _ => null,
        };
    }

    private static uint? WeekOfMonth(RecurrenceSpec spec) =>
        spec.BySetPosition is [var position]
            ? position switch
            {
                -1 => LastWeekOfMonth,
                >= 1 and <= 4 => (uint)position,
                _ => null,
            }
            : null;

    private static uint? Occurrences(RecurrenceSpec spec) => spec.Count is { } count ? (uint)count : null;

    private static uint FirstDayOfWeek(RecurrenceSpec spec) =>
        (uint)(spec.WeekStart ?? System.DayOfWeek.Sunday);
}
