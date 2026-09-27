using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// Pulls the full WP7 catalog from the upstream Microsoft Update service into the local database:
// one SyncUpdates tree-walk gathers all update metadata (applicability is client-side, so the
// catalog is comprehensive), then GetExtendedUpdateInfo fills in files for every leaf, and the
// referenced package CABs are downloaded to the local volume.
public class Crawler(
    UpdatesDbContext db,
    WsusUpstreamClient upstream,
    ILogger<Crawler> logger)
{
    private const int ExtendedBatchSize = 50;
    private const int MaxSweeps = 32;
    private const int MaxEmptySweeps = 3;
    private const int MaxRoundsPerSweep = 512;
    private const int MaxCookieRenewals = 3;
    private const int HarvestReportCap = 400;

    private const string DeviceTargetingMarker = "DeviceTargetingInfo";
    private const string MobileOperatorMarker = "Value=\"MobileOperator\"";

    public async Task CrawlAsync()
    {
        var cookie = await upstream.GetFreshCookieAsync();
        var all = new ConcurrentDictionary<long, ParsedUpdate>();
        var persisted = await db.Updates.Select(u => u.RevisionId).ToHashSetAsync();

        await WalkCatalogAsync(cookie, all, persisted);
        await HarvestMatrixAsync();
        await FetchExtendedInfoAsync(await upstream.GetFreshCookieAsync());
        await ResolveFileLocationsAsync(await upstream.GetFreshCookieAsync());

        var narrowed = await CatalogWriter.RecomputeLeafFlagsAsync(db);
        logger.LogInformation("Leaf flags recomputed: {Count} revisions differ from what upstream reported", narrowed);
    }

    private async Task WalkCatalogAsync(WsusCookie cookie, ConcurrentDictionary<long, ParsedUpdate> all, HashSet<long> persisted)
    {
        var detectoids = new List<long>();  // non-leaf, non-category 
        var categories = new List<long>();  // category roots, reported alongside every detectoid set

        var empty = 0;
        for (var sweep = 1; sweep <= MaxSweeps; sweep++)
        {
            var gained = await SweepAsync(cookie, detectoids.ToList(), all, detectoids, categories);

            await PersistAsync(all.Values, persisted);

            logger.LogInformation("Sweep {Sweep}: +{Gain} ({Total} known, {Leaves} leaves, {Detectoids} detectoids)",
                sweep, gained, all.Count, all.Values.Count(u => u.IsLeaf), detectoids.Count);

            if (gained == 0)
                empty++;

            if (empty > MaxEmptySweeps)
                break;
        }
    }

    public async Task HarvestMatrixAsync()
    {
        var persisted = await db.Updates.Select(u => u.RevisionId).ToHashSetAsync();

        var backbone = await db.Updates
            .Where(u => !u.IsLeaf && (u.Metadata == null || !u.Metadata.SyncXml.Contains(DeviceTargetingMarker)))
            .Select(u => u.RevisionId).ToListAsync();

        var operators = await db.Updates
            .Where(u => !u.IsLeaf && u.Metadata != null && u.Metadata.SyncXml.Contains(MobileOperatorMarker))
            .Select(u => u.RevisionId).ToListAsync();

        // everything device-targeting that isn't an operator detectoid (OEMDeviceName, and the rare
        // Manufacturer/Model)
        var operatorSet = operators.ToHashSet();
        var devices = (await db.Updates
            .Where(u => !u.IsLeaf && u.Metadata != null && u.Metadata.SyncXml.Contains(DeviceTargetingMarker))
            .Select(u => u.RevisionId).ToListAsync())
            .Where(r => !operatorSet.Contains(r)).ToList();

        var budget = Math.Max(2, HarvestReportCap - backbone.Count);
        var opChunkSize = Math.Max(1, Math.Min(operators.Count, budget / 2));
        var devChunkSize = Math.Max(1, budget - opChunkSize);

        var opChunks = operators.Count == 0 ? [[]] : operators.Chunk(opChunkSize).ToList();
        var devChunks = devices.Chunk(devChunkSize).ToList();

        logger.LogInformation("Matrix harvest: backbone={Backbone}, devices={Devices} in {DC} chunks, operators={Operators} in {OC} chunks -> {Walks} walks",
            backbone.Count, devices.Count, devChunks.Count, operators.Count, opChunks.Count, devChunks.Count * opChunks.Count);

        var totalNew = 0;
        var walk = 0;
        var walks = devChunks.Count * opChunks.Count;
        foreach (var devChunk in devChunks)
        {
            foreach (var opChunk in opChunks)
            {
                walk++;
                var reported = new List<long>(backbone.Count + devChunk.Length + opChunk.Length);
                reported.AddRange(backbone);
                reported.AddRange(devChunk);
                reported.AddRange(opChunk);

                var result = await CollectAsync(reported);

                var newUpdates = result.Seen.Values
                    .Where(u => u.UpdateId != Guid.Empty && !persisted.Contains(u.RevisionId))
                    .ToList();
                await PersistAsync(newUpdates, persisted);
                totalNew += newUpdates.Count;

                logger.LogInformation("Matrix walk {N}/{Total}: report={Report} -> {Seen} seen, +{New} new ({TotalNew} total new){Fault}",
                    walk, walks, reported.Count, result.Seen.Count, newUpdates.Count, totalNew,
                    result.Faulted ? " FAULTED" : "");
            }
        }
    }

    private async Task<int> SweepAsync(WsusCookie cookie, List<long> detectoidSet, ConcurrentDictionary<long, ParsedUpdate> all, List<long> detectoids, List<long> categories)
    {
        var reported = new List<long>(categories.Count + detectoidSet.Count);
        reported.AddRange(categories);
        reported.AddRange(detectoidSet);

        var localCached = new List<long>();
        var localSeen = new HashSet<long>();
        var gained = 0;
        var renewals = 0;

        var sessionCookie = cookie;
        var currentCookie = cookie;
        for (var round = 1; round <= MaxRoundsPerSweep; round++)
        {
            var responseXml = await upstream.SyncUpdatesAsync(currentCookie, reported, localCached);

            var fault = WsupXml.ReadFaultCode(responseXml);
            if (fault is not null)
            {
                if (WsupXml.IsCookieFault(fault))
                {
                    if (++renewals > MaxCookieRenewals)
                    {
                        logger.LogError("SyncUpdates still faulting {Fault} after {Renewals} cookie renewals, abandoning sweep of {Count} detectoids",
                            fault, MaxCookieRenewals, detectoidSet.Count);
                        return gained;
                    }

                    logger.LogInformation("Renewing cookie after {Fault}", fault);
                    sessionCookie = await upstream.GetFreshCookieAsync();
                    currentCookie = sessionCookie;
                    continue;
                }

                if (detectoidSet.Count <= 1)
                {
                    logger.LogWarning("Irreducible SyncUpdates fault {Fault} reporting {Reported} ids: {Body}",
                        fault, reported.Count, responseXml);
                    return gained;
                }

                var half = detectoidSet.Count / 2;
                logger.LogInformation("SyncUpdates fault {Fault} reporting {Reported} ids, splitting {Count} detectoids into {Left}+{Right}",
                    fault, reported.Count, detectoidSet.Count, half, detectoidSet.Count - half);

                gained += await SweepAsync(sessionCookie, [.. detectoidSet.Take(half)], all, detectoids, categories);
                gained += await SweepAsync(sessionCookie, [.. detectoidSet.Skip(half)], all, detectoids, categories);
                return gained;
            }

            var newThisRound = 0;
            foreach (var u in SyncUpdatesParser.ParseResponse(responseXml))
            {
                if (u.UpdateId == Guid.Empty && !all.ContainsKey(u.RevisionId))
                {
                    if (localSeen.Add(u.RevisionId))
                    {
                        newThisRound++;
                        localCached.Add(u.RevisionId);
                    }
                    continue;
                }

                if (!all.TryGetValue(u.RevisionId, out var existing))
                {
                    all[u.RevisionId] = u;
                    gained++;
                    if (!u.IsLeaf)
                        (u.UpdateType == UpdateType.Category ? categories : detectoids).Add(u.RevisionId);
                }
                else if (string.IsNullOrEmpty(existing.SyncMetadataXml) && !string.IsNullOrEmpty(u.SyncMetadataXml))
                {
                    all[u.RevisionId] = u;
                }

                if (localSeen.Add(u.RevisionId))
                {
                    newThisRound++;
                    localCached.Add(u.RevisionId);
                }
            }

            if (responseXml.Contains("<Truncated>true</Truncated>", StringComparison.Ordinal))
            {
                currentCookie = WsusCookie.Parse(responseXml)!;
                continue;
            }

            currentCookie = sessionCookie;

            if (newThisRound == 0)
                return gained;
        }

        logger.LogWarning("Sweep of {Count} detectoids hit the {Max} round cap without settling",
            detectoidSet.Count, MaxRoundsPerSweep);

        return gained;
    }


    private async Task PersistAsync(IEnumerable<ParsedUpdate> updates, HashSet<long> persisted)
    {
        var added = 0;
        foreach (var parsed in updates)
        {
            if (!persisted.Add(parsed.RevisionId))
                continue;

            db.Updates.Add(CatalogWriter.BuildUpdate(parsed, UpdateOrigin.Crawled));
            added++;
        }

        if (added == 0)
            return;

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private async Task FetchExtendedInfoAsync(WsusCookie cookie)
    {
        var pending = db.Updates
            .Where(u => u.ExtendedMetadata == null)
            .Select(u => u.RevisionId);

        await FetchExtendedInfoAsync(cookie, pending);
    }

    public async Task FetchExtendedInfoAsync(WsusCookie cookie, IQueryable<long> pending)
    {
        var pendingList = await pending.ToListAsync();
        logger.LogInformation("Fetching extended info for {Count} updates", pendingList.Count);

        foreach (var batch in pendingList.Chunk(ExtendedBatchSize))
        {
            var responseXml = await upstream.GetExtendedUpdateInfoAsync(cookie, batch);
            var extended = ExtendedUpdateInfoParser.ParseResponse(responseXml);

            foreach (var ext in extended)
            {
                var update = await db.Updates
                    .Include(u => u.Files)
                    .Include(u => u.Localizations)
                    .Include(u => u.Fragments)
                    .Include(u => u.ExtendedMetadata)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(u => u.RevisionId == ext.RevisionId);
                if (update is null)
                    continue;

                CatalogWriter.ApplyExtendedInfo(update, ext);
            }

            await db.SaveChangesAsync();
        }
    }

    public async Task ResolveFileLocationsAsync(WsusCookie cookie)
    {
        var digests = await db.Files
            .Where(f => f.SourceUrl == null && f.DigestSha1 != null)
            .Select(f => f.DigestSha1!)
            .Distinct()
            .ToListAsync();

        if (digests.Count == 0)
        {
            logger.LogInformation("ResolveFileLocations: no files missing URLs");
            return;
        }

        logger.LogInformation("ResolveFileLocations: {Count} digests to resolve", digests.Count);

        var urlByDigest = new Dictionary<string, string>();
        foreach (var batch in digests.Chunk(100))
        {
            var responseXml = await upstream.GetFileLocationsAsync(cookie, batch);
            if (responseXml.Contains("<ErrorCode>", StringComparison.Ordinal))
            {
                logger.LogWarning("GetFileLocations fault for a batch of {Count}", batch.Length);
                continue;
            }
            foreach (var (digest, url) in WsupXml.ParseFileLocations(responseXml))
                urlByDigest[digest] = url;
        }

        var resolved = 0;
        foreach (var file in await db.Files.Where(f => f.SourceUrl == null && f.DigestSha1 != null).ToListAsync())
        {
            if (file.DigestSha1 is not null && urlByDigest.TryGetValue(file.DigestSha1, out var url))
            {
                file.SourceUrl = url;
                file.LocalPath = Path.GetFileName(new Uri(url).LocalPath);
                resolved++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("ResolveFileLocations: resolved {Resolved}/{Total} files", resolved, digests.Count);
    }

    private async Task<ProbeResult> CollectAsync(IReadOnlyCollection<long> reported)
    {
        var cookie = await upstream.GetFreshCookieAsync();
        var seen = new Dictionary<long, ParsedUpdate>();
        var localCached = new List<long>();
        var truncatedRounds = 0;
        var faulted = false;

        var currentCookie = cookie;
        for (var round = 1; round <= 256; round++)
        {
            var responseXml = await upstream.SyncUpdatesAsync(currentCookie, reported, localCached);

            if (responseXml.Contains("<ErrorCode>", StringComparison.Ordinal))
            {
                faulted = true;
                break;
            }

            var newThisRound = 0;
            foreach (var u in SyncUpdatesParser.ParseResponse(responseXml))
            {
                if (seen.TryAdd(u.RevisionId, u))
                    newThisRound++;
                localCached.Add(u.RevisionId);
            }

            var truncated = responseXml.Contains("<Truncated>true</Truncated>", StringComparison.Ordinal);
            if (truncated)
            {
                truncatedRounds++;
                currentCookie = WsusCookie.Parse(responseXml)!;
                continue;
            }

            currentCookie = cookie;
            if (newThisRound == 0)
                break;
        }

        return new ProbeResult(seen, truncatedRounds, faulted);
    }

    private record ProbeResult(Dictionary<long, ParsedUpdate> Seen, int TruncatedRounds, bool Faulted);
}
