using System.Globalization;

namespace ReLiveWP.Services.Exchange.Helpers;

// two wire formats exist, and the field's owning class decides which is legal: extended
// (FromDateTime) for Tasks and Contacts, compact (FromDateTimeCompact) for Calendar. Emitting
// extended for Calendar makes strict clients (e.g. iOS) reject the item and re-prime. Reads
// accept both forms regardless of class since WP7 emits compact for the fields it sends.
internal static class EasDateHelper
{
    private const string WriteFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    private const string CompactWriteFormat = "yyyyMMddTHHmmssZ";

    // most-specific first
    private static readonly string[] ReadFormats =
    [
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyyMMddTHHmmssZ",
    ];

    public static string? FromDateTime(DateTime? dt) =>
        dt?.ToUniversalTime().ToString(WriteFormat, CultureInfo.InvariantCulture);

    public static string? FromDateTimeCompact(DateTime? dt) =>
        dt?.ToUniversalTime().ToString(CompactWriteFormat, CultureInfo.InvariantCulture);

    public static DateTime? ToDateTime(string? s) =>
        s is null ? null
        : DateTime.TryParseExact(s, ReadFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d
            : null;
}
