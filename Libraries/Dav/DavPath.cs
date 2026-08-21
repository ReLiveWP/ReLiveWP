namespace ReLiveWP.Dav;

public static class DavPath
{
    // Unescapes, so the result is a display/filesystem path rather than a URL. Do not use it to
    // derive anything persisted as a key: %20 and a space are the same path but different ids.
    public static string Normalise(string href)
    {
        var value = href.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            value = absolute.AbsolutePath;

        return '/' + Uri.UnescapeDataString(value).Trim('/');
    }

    public static string Encode(string path)
        => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    public static string? RelativeTo(string path, string root)
    {
        var prefix = root.Trim('/');

        if (prefix.Length == 0)
            return path.Trim('/') is { Length: > 0 } bare ? bare : null;

        var marker = '/' + prefix + '/';
        var index = path.LastIndexOf(marker, StringComparison.Ordinal);

        if (index < 0)
            return null;

        return path[(index + marker.Length)..].Trim('/') is { Length: > 0 } below ? below : null;
    }

    // Keeps the href escaped, because callers persist the result as an item id.
    public static string StripPrefix(string href, string? rootUrl)
    {
        var path = Uri.TryCreate(href, UriKind.Absolute, out var absolute) &&
                   (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)
            ? absolute.AbsolutePath
            : href;

        if (!string.IsNullOrEmpty(rootUrl) && Uri.TryCreate(rootUrl, UriKind.Absolute, out var root))
        {
            var prefix = root.AbsolutePath.TrimEnd('/');

            if (prefix.Length > 0 && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                path = path[prefix.Length..];
        }

        return path.TrimStart('/');
    }
}
