using System.Globalization;
using System.Xml.Linq;

namespace ReLiveWP.Backend.SkyDrive.Services;

public record WebDavEntry(
    string Path,
    bool IsCollection,
    string? ContentType,
    DateTimeOffset Modified,
    string? ETag,
    long Length);

public static class WebDavMultiStatus
{
    private static readonly XNamespace Dav = "DAV:";

    public static IReadOnlyList<WebDavEntry> Parse(string xml)
    {
        var entries = new List<WebDavEntry>();

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return entries;
        }

        foreach (var response in document.Descendants(Dav + "response"))
        {
            var href = response.Element(Dav + "href")?.Value;
            if (string.IsNullOrWhiteSpace(href))
                continue;

            var props = response.Elements(Dav + "propstat")
                .Where(IsOk)
                .Select(p => p.Element(Dav + "prop"))
                .OfType<XElement>()
                .ToList();

            var isCollection = props.Any(p => p.Element(Dav + "resourcetype")?.Element(Dav + "collection") != null);

            entries.Add(new WebDavEntry(
                NormalisePath(href),
                isCollection,
                First(props, "getcontenttype"),
                ParseModified(First(props, "getlastmodified")),
                First(props, "getetag"),
                long.TryParse(First(props, "getcontentlength"), out var length) ? length : 0));
        }

        return entries;
    }

    private static bool IsOk(XElement propstat)
    {
        var status = propstat.Element(Dav + "status")?.Value;
        return string.IsNullOrEmpty(status) || status.Contains(" 200", StringComparison.Ordinal);
    }

    private static string? First(List<XElement> props, string name)
        => props.Select(p => p.Element(Dav + name)?.Value)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static DateTimeOffset ParseModified(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : DateTimeOffset.UnixEpoch;

    public static string NormalisePath(string href)
    {
        var value = href.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            value = absolute.AbsolutePath;

        value = Uri.UnescapeDataString(value);

        return '/' + value.Trim('/');
    }
    
    public static string? RelativeToAlbum(string entryPath, string albumPath)
    {
        var album = albumPath.Trim('/');
        if (album.Length == 0)
            return entryPath.Trim('/') is { Length: > 0 } bare ? bare : null;

        var marker = '/' + album + '/';
        var index = entryPath.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            return null;

        return entryPath[(index + marker.Length)..].Trim('/') is { Length: > 0 } relative ? relative : null;
    }
}
