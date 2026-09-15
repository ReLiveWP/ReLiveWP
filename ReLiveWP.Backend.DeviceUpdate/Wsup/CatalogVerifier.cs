using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public record UnresolvedReference(long RevisionId, Guid MissingUpdateId, int GroupId);

public record CatalogIntegrityReport
{
    public required int UpdateCount { get; init; }
    public required IReadOnlyList<UnresolvedReference> UnresolvedPrerequisites { get; init; }
    public required IReadOnlyList<UnresolvedReference> UnresolvedBundledUpdates { get; init; }
    public required IReadOnlyList<long> UnsatisfiableRevisions { get; init; }
    public required IReadOnlyList<long> FilesMissingUrl { get; init; }
    public required IReadOnlyList<long> LeavesMissingExtendedMetadata { get; init; }
    public required IReadOnlyList<long> OrphanedBundledRevisions { get; init; }
    public required bool RingMarkerPresent { get; init; }
    public required bool RingMarkerIsLeaf { get; init; }

    public bool IsClean =>
        UnresolvedPrerequisites.Count == 0
        && UnresolvedBundledUpdates.Count == 0
        && FilesMissingUrl.Count == 0
        && LeavesMissingExtendedMetadata.Count == 0
        && RingMarkerPresent
        && !RingMarkerIsLeaf;
}

public static class CatalogVerifier
{
    public static async Task<CatalogIntegrityReport> CheckCatalogAsync(UpdatesDbContext db)
    {
        var known = await db.Updates.Select(u => u.UpdateId).Distinct().ToHashSetAsync();

        var prerequisites = await db.Prerequisites
            .Select(p => new { p.UpdateRevisionId, p.PrerequisiteUpdateId, p.GroupId })
            .ToListAsync();

        var bundles = await db.Bundles
            .Select(b => new { b.BundleRevisionId, b.BundledUpdateId })
            .ToListAsync();

        var unresolvedPrerequisites = prerequisites
            .Where(p => !known.Contains(p.PrerequisiteUpdateId))
            .Select(p => new UnresolvedReference(p.UpdateRevisionId, p.PrerequisiteUpdateId, p.GroupId))
            .ToList();

        var unresolvedBundles = bundles
            .Where(b => !known.Contains(b.BundledUpdateId))
            .Select(b => new UnresolvedReference(b.BundleRevisionId, b.BundledUpdateId, 0))
            .ToList();

        var unsatisfiable = new HashSet<long>(unresolvedBundles.Select(b => b.RevisionId));

        foreach (var group in prerequisites.GroupBy(p => p.UpdateRevisionId))
        {
            var mandatoryMissing = group
                .Where(p => p.GroupId == 0)
                .Any(p => !known.Contains(p.PrerequisiteUpdateId));

            var groupExhausted = group
                .Where(p => p.GroupId > 0)
                .GroupBy(p => p.GroupId)
                .Any(g => g.All(p => !known.Contains(p.PrerequisiteUpdateId)));

            if (mandatoryMissing || groupExhausted)
                unsatisfiable.Add(group.Key);
        }

        var actions = await db.Updates
            .Select(u => new { u.RevisionId, u.UpdateId, u.DeploymentAction })
            .ToListAsync();

        var revisionsByGuid = actions
            .GroupBy(u => u.UpdateId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.RevisionId).ToList());

        var parentsByChild = bundles
            .GroupBy(b => b.BundledUpdateId)
            .ToDictionary(g => g.Key, g => g.Select(b => b.BundleRevisionId).ToHashSet());

        var bundled = actions.Where(u => u.DeploymentAction == UpdateScopeFilter.BundleAction).ToList();

        bool grew;
        do
        {
            grew = false;
            foreach (var child in bundled)
            {
                if (unsatisfiable.Contains(child.RevisionId))
                    continue;
                if (!parentsByChild.TryGetValue(child.UpdateId, out var parents) || parents.Count == 0)
                    continue;
                if (parents.All(unsatisfiable.Contains))
                    grew |= unsatisfiable.Add(child.RevisionId);
            }
        }
        while (grew);

        var filesMissingUrl = await db.Files
            .Where(f => f.SourceUrl == null)
            .Select(f => f.UpdateRevisionId)
            .Distinct()
            .ToListAsync();

        var bundledGuids = bundles.Select(b => b.BundledUpdateId).ToHashSet();
        var orphanedBundled = (await db.Updates
            .Where(u => u.DeploymentAction == UpdateScopeFilter.BundleAction)
            .Select(u => new { u.RevisionId, u.UpdateId })
            .ToListAsync())
            .Where(u => !bundledGuids.Contains(u.UpdateId))
            .Select(u => u.RevisionId)
            .ToList();

        var leavesMissingMetadata = await db.Updates
            .Where(u => u.IsLeaf && u.UpdateType == UpdateType.Software && u.ExtendedMetadata == null)
            .Select(u => u.RevisionId)
            .ToListAsync();

        var markerRevisions = await db.Updates
            .Where(u => u.UpdateId == DeviceRingResolver.MarkerUpdateId)
            .Select(u => u.IsLeaf)
            .ToListAsync();

        return new CatalogIntegrityReport
        {
            UpdateCount = await db.Updates.CountAsync(),
            RingMarkerPresent = markerRevisions.Count > 0,
            RingMarkerIsLeaf = markerRevisions.Any(isLeaf => isLeaf),
            UnresolvedPrerequisites = unresolvedPrerequisites,
            UnresolvedBundledUpdates = unresolvedBundles,
            UnsatisfiableRevisions = [.. unsatisfiable.Order()],
            FilesMissingUrl = filesMissingUrl,
            LeavesMissingExtendedMetadata = leavesMissingMetadata,
            OrphanedBundledRevisions = orphanedBundled,
        };
    }

    public static void LogReport(CatalogIntegrityReport report, ILogger logger)
    {
        if (report.IsClean)
            logger.LogInformation("Catalog integrity: {Count} updates, no unresolved references", report.UpdateCount);

        if (!report.RingMarkerPresent)
            logger.LogError("Ring marker detectoid {UpdateId} is not in the catalog, so every device resolves Unknown. Run 'author'.",
                DeviceRingResolver.MarkerUpdateId);

        if (report.RingMarkerIsLeaf)
            logger.LogError("Ring marker detectoid {UpdateId} is a leaf, so the client will never report it.",
                DeviceRingResolver.MarkerUpdateId);

        foreach (var missing in report.UnresolvedPrerequisites
            .GroupBy(p => p.MissingUpdateId)
            .OrderByDescending(g => g.Count()))
        {
            logger.LogWarning("Prerequisite {UpdateId} is referenced by {Count} revision(s) but is not in the catalog",
                missing.Key, missing.Count());
        }

        foreach (var missing in report.UnresolvedBundledUpdates
            .GroupBy(b => b.MissingUpdateId)
            .OrderByDescending(g => g.Count()))
        {
            logger.LogWarning("Bundled update {UpdateId} is referenced by {Count} bundle(s) but is not in the catalog",
                missing.Key, missing.Count());
        }

        if (report.UnsatisfiableRevisions.Count > 0)
        {
            logger.LogError("{Count} of {Total} revisions can never be offered to a device because a requirement is missing from the catalog: {Revisions}",
                report.UnsatisfiableRevisions.Count, report.UpdateCount,
                string.Join(", ", report.UnsatisfiableRevisions.Take(20)));
        }

        if (report.FilesMissingUrl.Count > 0)
            logger.LogWarning("{Count} revision(s) have files with no download URL", report.FilesMissingUrl.Count);

        if (report.LeavesMissingExtendedMetadata.Count > 0)
            logger.LogWarning("{Count} software leaf revision(s) have no extended metadata", report.LeavesMissingExtendedMetadata.Count);

        if (report.OrphanedBundledRevisions.Count > 0)
            logger.LogWarning("{Count} revision(s) are deployed as Bundle but nothing bundles them", report.OrphanedBundledRevisions.Count);
    }
}
