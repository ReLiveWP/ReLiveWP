using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// The one place a parse result becomes catalog rows, so an authored revision is indistinguishable
// from a crawled one on disk rather than only by careful parallel maintenance.
public static class CatalogWriter
{
    public static Update BuildUpdate(ParsedUpdate parsed, UpdateOrigin origin) => new()
    {
        RevisionId = parsed.RevisionId,
        UpdateId = parsed.UpdateId,
        RevisionNumber = parsed.RevisionNumber,
        UpdateType = parsed.UpdateType,
        Origin = origin,
        IsLeafReported = parsed.IsLeaf,
        IsLeaf = parsed.IsLeaf,
        DeploymentAction = parsed.DeploymentAction,
        IsBundle = parsed.IsBundle,
        LastChangeTime = parsed.LastChangeTime,
        Metadata = new UpdateMetadata { SyncXml = parsed.SyncMetadataXml },
        // Deduplicate against the composite keys ({revision, prereq guid}, {revision, child}).
        Prerequisites = parsed.Prerequisites
            .DistinctBy(p => p.PrerequisiteUpdateId)
            .Select(p => new UpdatePrerequisite
            {
                PrerequisiteUpdateId = p.PrerequisiteUpdateId,
                GroupId = p.GroupId,
                IsCategory = p.IsCategory,
            }).ToList(),
        BundledUpdates = parsed.Bundles
            .DistinctBy(b => b.BundledUpdateId)
            .Select(b => new UpdateBundle
            {
                BundledUpdateId = b.BundledUpdateId,
                BundledRevisionNumber = b.BundledRevisionNumber,
            }).ToList(),
    };

    public static void ApplyExtendedInfo(Update entity, ParsedExtendedUpdate extended)
    {
        entity.ExtendedMetadata = new UpdateExtendedMetadata { Xml = extended.ExtendedMetadataXml };

        entity.Fragments = [.. extended.Fragments.Select(f => new UpdateFragment
        {
            FragmentType = f.FragmentType,
            Language = f.Language,
            Ordinal = f.Ordinal,
            Xml = f.Xml,
        })];

        entity.Files = extended.Files
            .DistinctBy(f => f.FileName)
            .Select(f => new UpdateFile
            {
                FileName = f.FileName,
                Size = f.Size,
                DigestSha1 = f.DigestSha1,
                DigestSha256 = f.DigestSha256,
                Modified = f.Modified,
                SourceUrl = f.SourceUrl,
                LocalPath = f.SourceUrl is null ? null : Path.GetFileName(new Uri(f.SourceUrl).LocalPath),
            }).ToList();

        entity.Localizations = extended.Localizations
            .DistinctBy(l => l.Language)
            .Select(l => new UpdateLocalization
            {
                Language = l.Language,
                Title = l.Title,
                Description = l.Description,
                MoreInfoUrl = l.MoreInfoUrl,
                SupportUrl = l.SupportUrl,
            }).ToList();
    }

    // IsLeaf drives the client's call-again rule and decides what comes back in
    // InstalledNonLeafUpdateIDs, so it is defined over the catalog we actually serve. A revision is
    // a leaf only when nothing here names it as a prerequisite AND upstream agreed; over-reporting
    // costs a satisfied GUID nobody asked about, under-reporting strands every dependent forever.
    public static async Task<int> RecomputeLeafFlagsAsync(UpdatesDbContext db)
    {
        var referenced = await db.Prerequisites
            .Select(p => p.PrerequisiteUpdateId)
            .Distinct()
            .ToListAsync();

        var referencedIds = referenced.ToHashSet();
        var changed = 0;

        foreach (var update in await db.Updates.ToListAsync())
        {
            var isLeaf = update.IsLeafReported && !referencedIds.Contains(update.UpdateId);
            if (update.IsLeaf == isLeaf)
                continue;

            update.IsLeaf = isLeaf;
            changed++;
        }

        if (changed > 0)
            await db.SaveChangesAsync();

        return changed;
    }
}
