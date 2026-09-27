using System.Xml;
using System.Xml.Linq;

using static ReLiveWP.Dav.DavNamespaces;

namespace ReLiveWP.Dav;

public class DavMultiStatus
{
    private DavMultiStatus(XElement root, IReadOnlyList<DavResponse> responses, int skipped)
    {
        Root = root;
        Responses = responses;
        Skipped = skipped;
    }

    public XElement Root { get; }

    public IReadOnlyList<DavResponse> Responses { get; }

    // Responses dropped for having no href. A non-zero count means the server sent something we
    // could not address, which callers with an "unreadable" channel should report rather than bury.
    public int Skipped { get; }

    public string? SyncToken => (string?)Root.Element(DavProps.SyncToken);

    public IEnumerable<DavResponse> Found => Responses.Where(r => !r.IsNotFound);

    public IEnumerable<DavResponse> NotFound => Responses.Where(r => r.IsNotFound);

    public static DavMultiStatus Parse(string xml)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException e)
        {
            throw new DavParseException($"the server returned XML we could not read: {e.Message}", xml);
        }

        if (document.Root is not { } root || root.Name != WebDav + "multistatus")
            throw new DavParseException(
                $"the server returned {document.Root?.Name.LocalName ?? "nothing"}, expected multistatus", xml);

        var responses = new List<DavResponse>();
        var skipped = 0;

        foreach (var element in root.Elements(WebDav + "response"))
        {
            var href = (string?)element.Element(DavProps.Href);
            if (string.IsNullOrWhiteSpace(href))
            {
                skipped++;
                continue;
            }

            var props = element.Elements(WebDav + "propstat")
                               .Where(IsOk)
                               .Select(p => p.Element(WebDav + "prop"))
                               .OfType<XElement>()
                               .ToList();

            responses.Add(new DavResponse(href, StatusCode((string?)element.Element(DavProps.Status)), props, element));
        }

        return new DavMultiStatus(root, responses, skipped);
    }

    // A propstat with no status at all is technically malformed, but enough servers send one that
    // treating it as 200 is the difference between reading a share and not.
    private static bool IsOk(XElement propstat)
        => StatusCode((string?)propstat.Element(DavProps.Status)) is null or 200;

    private static int? StatusCode(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        foreach (var token in status.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length == 3 && int.TryParse(token, out var code))
                return code;
        }

        return null;
    }
}
