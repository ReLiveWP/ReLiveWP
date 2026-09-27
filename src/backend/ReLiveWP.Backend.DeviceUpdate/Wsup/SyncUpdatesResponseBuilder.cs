using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class SyncUpdatesResponseBuilder
{
    public static string Build(
        IReadOnlyList<CandidateRevision> updates,
        IReadOnlyList<CandidateRevision> changed,
        IReadOnlyDictionary<long, string> metadata,
        IReadOnlyList<long> outOfScope,
        bool truncated)
    {
        XNamespace ns = WsupXml.ServiceNs;

        var newUpdates = new XElement(ns + "NewUpdates",
            updates.Select(u => UpdateInfo(ns, u, metadata.GetValueOrDefault(u.RevisionId))));

        var changedUpdates = new XElement(ns + "ChangedUpdates",
            changed.Select(u => UpdateInfo(ns, u, null)));

        var cookie = WsusCookie.IssueCookie();

        var result = new XElement(ns + "SyncUpdatesResult", newUpdates);

        // SyncInfo declares these in order and the client is schema generated, so this sits between
        // NewUpdates and ChangedUpdates or it is not read. Omitted entirely when empty, because
        // upstream never sent the element at all and an empty one is untested on the device.
        if (outOfScope.Count > 0)
            result.Add(new XElement(ns + "OutOfScopeRevisionIDs",
                outOfScope.Select(id => new XElement(ns + "int", id))));

        result.Add(
            changedUpdates,
            new XElement(ns + "Truncated", truncated),
            cookie.ToResponse(ns, "NewCookie"),
            new XElement(ns + "DriverSyncNotNeeded", "false"));

        return WsupXml.BuildResponse("SyncUpdatesResponse", result);
    }

    private const long DeploymentIdOffset = 0x40000000;

    private static XElement UpdateInfo(XNamespace ns, CandidateRevision u, string? metadataXml)
    {
        var info = new XElement(ns + "UpdateInfo",
            new XElement(ns + "ID", u.RevisionId),
            new XElement(ns + "Deployment",
                new XElement(ns + "ID", u.RevisionId + DeploymentIdOffset),
                new XElement(ns + "Action", u.DeploymentAction),
                new XElement(ns + "IsAssigned", "true"),
                new XElement(ns + "LastChangeTime", (u.LastChangeTime ?? DateTime.UtcNow).ToString("yyyy-MM-dd")),
                new XElement(ns + "AutoSelect", "0"),
                new XElement(ns + "AutoDownload", "0"),
                new XElement(ns + "SupersedenceBehavior", "0")),
            new XElement(ns + "IsLeaf", u.IsLeaf));

        if (metadataXml is not null)
            info.Add(new XElement(ns + "Xml", metadataXml));

        return info;
    }
}
