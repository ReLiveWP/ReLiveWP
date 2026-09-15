using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// Seeds the database from the captured proxy responses: parses every SyncUpdates and
// GetExtendedUpdateInfo response, reduces to the WP7 catalog, and writes the entities.
public class CaptureImporter(UpdatesDbContext db, ILogger<CaptureImporter> logger)
{
    public async Task ImportAsync(string syncDir, string extendedDir)
    {
        // 1. Parse every SyncUpdates response; dedupe revisions across capture rounds. The same
        // revision recurs in multiple rounds, sometimes carrying the embedded <Xml> metadata and
        // sometimes bare (WSUS omits it once the client has cached it) — keep the metadata copy.
        var allUpdates = new Dictionary<long, ParsedUpdate>();
        foreach (var file in Directory.EnumerateFiles(syncDir, "*SyncUpdatesResponse.xml"))
        {
            foreach (var u in SyncUpdatesParser.ParseResponse(await File.ReadAllTextAsync(file)))
            {
                if (!allUpdates.TryGetValue(u.RevisionId, out var existing)
                    || (string.IsNullOrEmpty(existing.SyncMetadataXml) && !string.IsNullOrEmpty(u.SyncMetadataXml)))
                    allUpdates[u.RevisionId] = u;
            }
        }
        logger.LogInformation("Parsed {Count} distinct update revisions from {Dir}", allUpdates.Count, syncDir);

        // 2. Reduce to the WP7 catalog.
        var kept = Wp7Filter.Filter(allUpdates.Values.ToList());
        var keptIds = kept.Select(u => u.RevisionId).ToHashSet();
        logger.LogInformation("Kept {Kept} WP7 revisions (discarded {Discarded})", kept.Count, allUpdates.Count - kept.Count);

        // 3. Parse extended info for the kept revisions.
        var extended = new Dictionary<long, ParsedExtendedUpdate>();
        if (Directory.Exists(extendedDir))
        {
            foreach (var file in Directory.EnumerateFiles(extendedDir, "*GetExtendedUpdateInfoResponse.xml"))
            {
                foreach (var e in ExtendedUpdateInfoParser.ParseResponse(await File.ReadAllTextAsync(file)))
                    if (keptIds.Contains(e.RevisionId))
                        extended.TryAdd(e.RevisionId, e);
            }
        }
        logger.LogInformation("Matched extended info for {Count} revisions", extended.Count);

        // 4. Replace existing catalog with the freshly imported one.
        // await db.Updates.ExecuteDeleteAsync();

        foreach (var parsed in kept)
        {
            var entity = CatalogWriter.BuildUpdate(parsed, UpdateOrigin.Crawled);

            if (extended.TryGetValue(parsed.RevisionId, out var ext))
                CatalogWriter.ApplyExtendedInfo(entity, ext);

            db.Updates.Add(entity);
        }

        await db.SaveChangesAsync();
        await CatalogWriter.RecomputeLeafFlagsAsync(db);
        logger.LogInformation("Import complete: {Count} updates written", kept.Count);
    }
}
