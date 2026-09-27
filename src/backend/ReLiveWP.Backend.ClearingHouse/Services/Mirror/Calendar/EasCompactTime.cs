using System.Globalization;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public static class EasCompactTime
{
    private const string Format = "yyyyMMdd'T'HHmmss'Z'";

    // the same shape iCalendar calls basic format, which is why EXDATE and RECURRENCE-ID read here
    private static readonly string[] ReadFormats =
        [Format, "yyyyMMdd'T'HHmmss", "yyyyMMdd"];

    // unspecified means it already came off the wire as UTC, which is all the mirror ever deals in
    public static string From(DateTime value) => (value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => value,
    }).ToString(Format, CultureInfo.InvariantCulture);

    public static bool TryParse(string? value, out DateTime parsed) =>
        DateTime.TryParseExact(value, ReadFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed)
        || DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed);
}
