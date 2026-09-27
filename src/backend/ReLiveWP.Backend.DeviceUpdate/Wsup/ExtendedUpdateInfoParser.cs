using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class ExtendedUpdateInfoParser
{
    public static List<ParsedExtendedUpdate> ParseResponse(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        XNamespace ns = WsupXml.ServiceNs;

        // FileLocations maps a file's SHA1 digest to its download URL.
        var urlByDigest = new Dictionary<string, string>();
        foreach (var loc in doc.Descendants(ns + "FileLocation"))
        {
            var digest = (string?)loc.Element(ns + "FileDigest");
            var url = (string?)loc.Element(ns + "Url");
            if (digest is not null && url is not null)
                urlByDigest[digest] = url;
        }

        var byRevision = new Dictionary<long, ParsedExtendedUpdate>();
        foreach (var update in doc.Descendants(ns + "Update"))
        {
            var revisionId = (long)update.Element(ns + "ID")!;
            var inner = update.Element(ns + "Xml")?.Value ?? "";

            if (!byRevision.TryGetValue(revisionId, out var parsed))
            {
                parsed = new ParsedExtendedUpdate { RevisionId = revisionId };
                byRevision[revisionId] = parsed;
            }

            parsed.ExtendedMetadataXml += inner;
            parsed.Fragments.AddRange(ExtendedFragmentReader.ReadFragments(inner));
            ParseMetadata(inner, urlByDigest, parsed);
        }

        // ordinal is the position within the revision
        foreach (var parsed in byRevision.Values)
            for (var i = 0; i < parsed.Fragments.Count; i++)
                parsed.Fragments[i].Ordinal = i;

        return byRevision.Values.ToList();
    }

    private static void ParseMetadata(string innerXml, Dictionary<string, string> urlByDigest, ParsedExtendedUpdate parsed)
    {
        if (string.IsNullOrWhiteSpace(innerXml))
            return;

        var root = WsupXml.ParseFragment(innerXml);

        foreach (var file in root.Descendants().Where(e => e.Name.LocalName == "File"))
        {
            var sha1 = (string?)file.Attribute("Digest");
            var sha256 = file.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "AdditionalDigest"
                    && (string?)e.Attribute("Algorithm") == "SHA256")?.Value;

            parsed.Files.Add(new ParsedFile
            {
                FileName = (string?)file.Attribute("FileName") ?? "",
                Size = (long?)file.Attribute("Size") ?? 0,
                DigestSha1 = sha1,
                DigestSha256 = sha256,
                Modified = WsupXml.AsUtc((DateTime?)file.Attribute("Modified")),
                SourceUrl = sha1 is not null && urlByDigest.TryGetValue(sha1, out var url) ? url : null,
            });
        }

        foreach (var loc in root.Descendants().Where(e => e.Name.LocalName == "LocalizedProperties"))
        {
            string? Child(string name) => loc.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
            parsed.Localizations.Add(new ParsedLocalization
            {
                Language = Child("Language") ?? "",
                Title = Child("Title"),
                Description = Child("Description"),
                MoreInfoUrl = Child("MoreInfoUrl"),
                SupportUrl = Child("SupportUrl"),
            });
        }
    }
}
