using System.Globalization;

namespace ReLiveWP.Services.Activity.Utilities;

// the browser holds cids as the x16 hex EAS hands out, the atom routes speak decimal
public static class WebCids
{
    public static string FormatCid(long cid) => cid.ToString("x16", CultureInfo.InvariantCulture);

    public static string? FormatCid(long? cid) => cid is { } value ? FormatCid(value) : null;

    public static bool TryParseCid(string text, out long cid)
    {
        cid = 0;
        if (text.Length is 0 or > 16)
            return false;

        return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cid);
    }
}
