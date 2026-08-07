namespace ReLiveWP.Services.Docs.Dav;

public static class DavUrls
{
    public const string Prefix = "/dav";

    public static string Build(string baseUri, string path)
    {
        var trimmed = path.Trim('/');
        if (trimmed.Length == 0)
            return $"{baseUri}{Prefix}";

        var escaped = string.Join('/', trimmed.Split('/').Select(Uri.EscapeDataString));
        return $"{baseUri}{Prefix}/{escaped}";
    }

    public static string ToPath(string? davUrl)
    {
        if (string.IsNullOrWhiteSpace(davUrl))
            return "";

        var value = davUrl;

        if (Uri.TryCreate(davUrl, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;

        var marker = value.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
            value = value[(marker + Prefix.Length)..];

        return Uri.UnescapeDataString(value).Trim('/');
    }

    public static string Name(string path)
    {
        var trimmed = path.Trim('/');
        var slash = trimmed.LastIndexOf('/');
        return slash < 0 ? trimmed : trimmed[(slash + 1)..];
    }
}
