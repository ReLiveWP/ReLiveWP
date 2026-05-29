using System.Globalization;

namespace ReLiveWP.Services.Exchange.Models;

// The EAS datetime wire format used by Anniversary and Birthday.
// The time portion SHOULD be ignored per spec; we normalise to midnight UTC on read.
internal static class EasDate
{
    private const string Format = "yyyyMMddTHHmmssZ";

    public static string? FromDateTime(DateTime? dt) =>
        dt?.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);

    public static DateTime? ToDateTime(string? s) =>
        s is null ? null
        : DateTime.TryParseExact(s, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d.Date   // drop time component
            : null;
}