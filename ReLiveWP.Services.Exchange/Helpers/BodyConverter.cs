using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ReLiveWP.Services.Exchange.Helpers;

internal static partial class BodyConverter
{
    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyle();

    [GeneratedRegex(@"<\s*(br|/p|/div|/tr|/li|/h[1-6]|/blockquote)\s*/?\s*>",
        RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreak();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex HorizontalSpace();

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex ExcessBlankLines();

    public static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        var s = ScriptOrStyle().Replace(html, string.Empty);
        s = BlockBreak().Replace(s, "\n");
        s = AnyTag().Replace(s, string.Empty);
        s = WebUtility.HtmlDecode(s);

        // tag removal leaves runs of layout whitespace behind
        s = HorizontalSpace().Replace(s, " ");
        s = ExcessBlankLines().Replace(s, "\n\n");

        return s.Trim();
    }

    public static string PlainTextToHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var escaped = WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br>");
        return $"<html><body>{escaped}</body></html>";
    }
    
    public static int ByteLength(string? value) =>
        value is null ? 0 : Encoding.UTF8.GetByteCount(value);
    
    public static string TruncateToBytes(string value, int maxBytes)
    {
        if (maxBytes <= 0) return string.Empty;
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes) return value;

        var buf = Encoding.UTF8.GetBytes(value);
        int len = Math.Min(maxBytes, buf.Length);
        while (len > 0 && (buf[len] & 0xC0) == 0x80) len--;

        return Encoding.UTF8.GetString(buf, 0, len);
    }
}
