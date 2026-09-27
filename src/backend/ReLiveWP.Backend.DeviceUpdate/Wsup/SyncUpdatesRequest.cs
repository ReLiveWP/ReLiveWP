using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public class SyncUpdatesRequest
{
    public HashSet<long> InstalledNonLeafUpdateIds { get; } = [];
    public HashSet<long> OtherCachedUpdateIds { get; } = [];

    public HashSet<long> Known => [.. InstalledNonLeafUpdateIds, .. OtherCachedUpdateIds];

    public static SyncUpdatesRequest Parse(string requestXml)
    {
        var doc = XDocument.Parse(requestXml);
        var request = new SyncUpdatesRequest();

        ReadInts(doc, "InstalledNonLeafUpdateIDs", request.InstalledNonLeafUpdateIds);
        ReadInts(doc, "OtherCachedUpdateIDs", request.OtherCachedUpdateIds);

        return request;
    }

    private static void ReadInts(XDocument doc, string elementName, HashSet<long> into)
    {
        var container = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == elementName);
        if (container is null)
            return;

        foreach (var i in container.Elements().Where(e => e.Name.LocalName == "int"))
            if (long.TryParse(i.Value, out var value))
                into.Add(value);
    }
}
