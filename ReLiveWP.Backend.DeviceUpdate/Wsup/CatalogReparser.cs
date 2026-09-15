using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// Rebuilds everything derived from the stored metadata blobs, so a parser fix lands without
// re-crawling upstream. It has already recovered 219 dropped bundle edges and 97165 fragments that
// would otherwise have cost a full crawl each.
public class CatalogReparser(UpdatesDbContext db, ILogger<CatalogReparser> logger)
{
    public async Task ReparseAsync()
    {
        var prerequisites = 0;
        var bundles = 0;
        var fragments = 0;

        var updates = await db.Updates
            .Include(u => u.Prerequisites)
            .Include(u => u.BundledUpdates)
            .Include(u => u.Fragments)
            .Include(u => u.Metadata)
            .Include(u => u.ExtendedMetadata)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var update in updates)
        {
            if (update.ExtendedMetadata is not null)
            {
                update.Fragments = [.. ExtendedFragmentReader.ReadFragments(update.ExtendedMetadata.Xml)
                    .Select(f => new UpdateFragment
                    {
                        FragmentType = f.FragmentType,
                        Language = f.Language,
                        Ordinal = f.Ordinal,
                        Xml = f.Xml,
                    })];
                fragments += update.Fragments.Count;
            }

            if (string.IsNullOrEmpty(update.Metadata?.SyncXml))
                continue;

            var parsed = SyncUpdatesParser.ParseMetadataOnly(update.Metadata.SyncXml);

            // Deduplicate by the second key column: a prerequisite GUID can appear in more than one
            // group, and a bundle can list the same child twice.
            update.Prerequisites = parsed.Prerequisites
                .DistinctBy(p => p.PrerequisiteUpdateId)
                .Select(p => new UpdatePrerequisite
                {
                    PrerequisiteUpdateId = p.PrerequisiteUpdateId,
                    GroupId = p.GroupId,
                    IsCategory = p.IsCategory,
                }).ToList();

            update.BundledUpdates = parsed.Bundles
                .DistinctBy(b => b.BundledUpdateId)
                .Select(b => new UpdateBundle
                {
                    BundledUpdateId = b.BundledUpdateId,
                    BundledRevisionNumber = b.BundledRevisionNumber,
                }).ToList();

            prerequisites += update.Prerequisites.Count;
            bundles += update.BundledUpdates.Count;
        }

        await db.SaveChangesAsync();
        var narrowed = await CatalogWriter.RecomputeLeafFlagsAsync(db);

        logger.LogInformation("Reparsed: {Prerequisites} prerequisites, {Bundles} bundle edges, {Fragments} metadata fragments, {Narrowed} leaf flags narrowed",
            prerequisites, bundles, fragments, narrowed);
    }
}
