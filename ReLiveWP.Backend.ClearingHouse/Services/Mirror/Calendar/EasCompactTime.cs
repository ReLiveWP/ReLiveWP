using System.Globalization;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public static class EasCompactTime
{
    private const string Format = "yyyyMMdd'T'HHmmss'Z'";

    // unspecified means it already came off the wire as UTC, which is all the mirror ever deals in
    public static string From(DateTime value) => (value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => value,
    }).ToString(Format, CultureInfo.InvariantCulture);
}
