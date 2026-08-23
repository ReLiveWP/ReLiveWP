namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public enum RecurrenceFrequency
{
    Secondly,
    Minutely,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly,
}

// an RFC 5545 BYDAY element: SU..SA with an optional ordinal, where -1 counts back from the end
public readonly record struct WeekdayOrdinal(DayOfWeek Day, int? Ordinal);

// what both feeds normalise to. google and caldav parse an RRULE into this, graph builds it
// straight from its recurrencePattern, and only this shape ever reaches the mapper.
public sealed record RecurrenceSpec
{
    public required RecurrenceFrequency Frequency { get; init; }
    public int Interval { get; init; } = 1;
    public IReadOnlyList<WeekdayOrdinal> ByDay { get; init; } = [];
    public IReadOnlyList<int> ByMonthDay { get; init; } = [];
    public IReadOnlyList<int> ByMonth { get; init; } = [];
    public IReadOnlyList<int> BySetPosition { get; init; } = [];
    public IReadOnlyList<int> ByWeekNo { get; init; } = [];
    public IReadOnlyList<int> ByYearDay { get; init; } = [];
    public IReadOnlyList<int> ByHour { get; init; } = [];
    public IReadOnlyList<int> ByMinute { get; init; } = [];
    public IReadOnlyList<int> BySecond { get; init; } = [];
    public int? Count { get; init; }
    public DateTime? Until { get; init; }

    // MS-OXCICAL 2.1.3.2.1.9 defaults this to SU, which is not the RFC 5545 default of MO
    public DayOfWeek? WeekStart { get; init; }
}
