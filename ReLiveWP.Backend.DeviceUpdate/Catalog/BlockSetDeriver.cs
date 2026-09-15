using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

public static class BlockSetDeriver
{
    public record BlockSet(
        IReadOnlyList<Guid> DeployableUpdateIds,
        IReadOnlyList<long> PayloadRevisions,
        IReadOnlyList<long> BundleRevisions);

    public static async Task<BlockSet> DeriveAsync(UpdatesDbContext db, Version minimumVersion)
    {
        var candidates = await db.Metadata
            .AsNoTracking()
            .Where(m => m.SyncXml.Contains("./DevDetail/SwV"))
            .Select(m => new { m.RevisionId, m.SyncXml })
            .ToListAsync();

        var payloads = candidates
            .Where(m => DeliveredVersion(m.SyncXml) is { } delivered && delivered >= minimumVersion)
            .Select(m => m.RevisionId)
            .ToHashSet();

        var updateIdByRevision = await db.Updates
            .AsNoTracking()
            .Select(u => new { u.RevisionId, u.UpdateId, u.DeploymentAction })
            .ToListAsync();

        var identityOf = updateIdByRevision.ToDictionary(u => u.RevisionId, u => u.UpdateId);
        var actionOf = updateIdByRevision.ToDictionary(u => u.RevisionId, u => u.DeploymentAction);

        var edges = await db.Bundles
            .AsNoTracking()
            .Select(b => new { b.BundleRevisionId, b.BundledUpdateId })
            .ToListAsync();

        var parentsByChild = edges
            .GroupBy(e => e.BundledUpdateId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.BundleRevisionId).ToList());

        var reached = new HashSet<long>(payloads);
        var frontier = new Queue<long>(payloads);

        while (frontier.Count > 0)
        {
            var revision = frontier.Dequeue();
            if (!identityOf.TryGetValue(revision, out var updateId))
                continue;
            if (!parentsByChild.TryGetValue(updateId, out var parents))
                continue;

            foreach (var parent in parents.Where(reached.Add))
                frontier.Enqueue(parent);
        }

        var ancestors = reached.Except(payloads).ToList();

        return new BlockSet(
            [.. ancestors
                .Where(r => actionOf.GetValueOrDefault(r) != UpdateScopeFilter.BundleAction)
                .Select(r => identityOf[r])
                .Distinct()
                .Order()],
            [.. payloads.Order()],
            [.. ancestors.Where(r => actionOf.GetValueOrDefault(r) == UpdateScopeFilter.BundleAction).Order()]);
    }


    private static Version? DeliveredVersion(string syncXml)
    {
        var installed = WsupXml.ParseFragment(syncXml)
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "ApplicabilityRules")?
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "IsInstalled");

        if (installed is null)
            return null;

        return installed
            .DescendantsAndSelf()
            .Where(e => e.Name.LocalName == "CspQuery")
            .Where(e => (string?)e.Attribute("LocUri") == "./DevDetail/SwV")
            .Where(e => (string?)e.Attribute("Comparison") == "GreaterThanOrEqualTo")
            .Select(e => Version.TryParse((string?)e.Attribute("Value"), out var version) ? version : null)
            .FirstOrDefault(version => version is not null);
    }
}
