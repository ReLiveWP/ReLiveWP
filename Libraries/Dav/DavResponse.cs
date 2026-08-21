using System.Globalization;
using System.Xml.Linq;

using static ReLiveWP.Dav.DavNamespaces;

namespace ReLiveWP.Dav;

public class DavResponse
{
    internal DavResponse(string href, int? status, IReadOnlyList<XElement> props, XElement element)
    {
        Href = href;
        Status = status;
        Props = props;
        Element = element;
    }

    public string Href { get; }

    // Unescaped, so it is safe to compare and display but not to persist. See DavPath.Normalise.
    public string Path => DavPath.Normalise(Href);

    public int? Status { get; }

    // The <prop> elements from every propstat the server reported as 200.
    public IReadOnlyList<XElement> Props { get; }

    public XElement Element { get; }

    public bool IsNotFound => Status == 404;

    public bool IsCollection => Props.Any(p => p.Element(DavProps.ResourceType)?.Element(DavProps.Collection) != null);

    public bool IsResourceType(XName name) => Props.Any(p => p.Element(DavProps.ResourceType)?.Element(name) != null);

    public string? ContentType => Value(DavProps.GetContentType);

    public string? ETag => Value(DavProps.GetETag);

    public string? DisplayName => Value(DavProps.DisplayName);

    public long Length => long.TryParse(Value(DavProps.GetContentLength), out var length) ? length : 0;

    public DateTimeOffset Modified =>
        DateTimeOffset.TryParse(Value(DavProps.GetLastModified), CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : DateTimeOffset.UnixEpoch;

    public XElement? Prop(XName name) => Props.Select(p => p.Element(name)).OfType<XElement>().FirstOrDefault();

    public string? Value(XName name) =>
        Props.Select(p => (string?)p.Element(name)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    // Some servers nest the property one level deeper than the schema suggests, so href lookups
    // search rather than index.
    public string? HrefValue(XName name) =>
        Props.SelectMany(p => p.Elements(name))
             .Descendants(WebDav + "href")
             .Select(x => (string?)x)
             .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
