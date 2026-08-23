using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public static class EasDayOfWeek
{
    public const uint Sunday = 1;
    public const uint Monday = 2;
    public const uint Tuesday = 4;
    public const uint Wednesday = 8;
    public const uint Thursday = 16;
    public const uint Friday = 32;
    public const uint Saturday = 64;

    // MS-ASCAL 2.2.2.15 gives these three sums their own meanings
    public const uint Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday;   // 62
    public const uint WeekendDays = Sunday | Saturday;                               // 65
    public const uint LastDayOfMonth = 127;

    public static uint Bit(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => Sunday,
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        _ => Saturday,
    };
}

public sealed record EasRecurrence
{
    public required uint Type { get; init; }
    public uint? Interval { get; init; }
    public uint? Occurrences { get; init; }
    public DateTime? Until { get; init; }
    public uint? DayOfWeek { get; init; }
    public uint? DayOfMonth { get; init; }
    public uint? WeekOfMonth { get; init; }
    public uint? MonthOfYear { get; init; }
    public uint? FirstDayOfWeek { get; init; }

    public void ApplyTo(CalendarItem item)
    {
        item.RecurrenceType = Type;

        if (Interval is { } interval) item.RecurrenceInterval = interval;
        if (Occurrences is { } occurrences) item.RecurrenceOccurrences = occurrences;
        if (Until is { } until)
            item.RecurrenceUntil = Timestamp.FromDateTime(DateTime.SpecifyKind(until, DateTimeKind.Utc));
        if (DayOfWeek is { } dayOfWeek) item.RecurrenceDayOfWeek = dayOfWeek;
        if (DayOfMonth is { } dayOfMonth) item.RecurrenceDayOfMonth = dayOfMonth;
        if (WeekOfMonth is { } weekOfMonth) item.RecurrenceWeekOfMonth = weekOfMonth;
        if (MonthOfYear is { } monthOfYear) item.RecurrenceMonthOfYear = monthOfYear;
        if (FirstDayOfWeek is { } firstDayOfWeek) item.RecurrenceFirstDayOfWeek = firstDayOfWeek;
    }
}

// mapped, or not representable and the reason why. a reason is a log line and an expansion, not an
// error, so it has to survive as data rather than an exception.
public readonly record struct RecurrenceMapping(EasRecurrence? Recurrence, string? Reason)
{
    public bool Mapped => Recurrence is not null;

    public static RecurrenceMapping Ok(EasRecurrence recurrence) => new(recurrence, null);

    public static RecurrenceMapping Unrepresentable(string reason) => new(null, reason);
}
