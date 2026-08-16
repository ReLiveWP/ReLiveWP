using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.Sync)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class SyncController : ActiveSyncCommandController
{
    private const int MinHeartbeatSeconds = 60;
    private const int MaxHeartbeatSeconds = 3540;
    private const int MinWaitMinutes = 1;
    private const int MaxWaitMinutes = 59;

    // MS-ASCMD Sync status table caps a request at 32 collections (Status 15)
    private const int MaxCollectionsPerRequest = 32;

    // Status 9 (out of disk space) is understood but not implemented: this server has no
    // mailbox-quota concept to detect it.

    private readonly ILogger<SyncController> _logger;
    private readonly ItemSyncService _itemSync;
    private readonly PushMonitor _monitor;
    private readonly ISyncRequestCache _cache;

    public SyncController(ILogger<SyncController> logger, ItemSyncService itemSync, PushMonitor monitor,
                          ISyncRequestCache cache)
    {
        _logger = logger;
        _itemSync = itemSync;
        _monitor = monitor;
        _cache = cache;
    }

    private sealed record Effective(Sync Request, List<SyncCollection> Collections, bool FromCache);

    private readonly record struct Resolved(Effective? Request, int Status);

    [HttpPost]
    public async Task Post()
    {
        var ct = HttpContext.RequestAborted;

        if (EasContext.BodyDecodeFailed)
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 4 }, _logger);
            return;
        }

        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<Sync>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        try
        {
            var resolved = await ResolveAsync(userId, request, ct);
            if (resolved.Request is null)
            {
                await WriteWbxmlResponseAsync(new Sync { Status = resolved.Status }, _logger);
                return;
            }

            await ExecuteAsync(userId, resolved.Request, ct);
        }
        catch (RpcException e) when (IsTransient(e))
        {
            _logger.LogWarning(e, "Sync: transient backend failure, asking client to retry");
            await WriteWbxmlResponseAsync(new Sync { Status = 16 }, _logger);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Sync: unexpected failure");
            await WriteWbxmlResponseAsync(new Sync { Status = 5 }, _logger);
        }
    }

    // Unavailable/DeadlineExceeded/Aborted/ResourceExhausted are worth a client retry (Status 16);
    // anything else is a genuine server error (Status 5)
    private static bool IsTransient(RpcException e) =>
        e.StatusCode is global::Grpc.Core.StatusCode.Unavailable or global::Grpc.Core.StatusCode.DeadlineExceeded
            or global::Grpc.Core.StatusCode.Aborted or global::Grpc.Core.StatusCode.ResourceExhausted;

    // MS-ASCMD 2.2.1.21.1 and 2.2.3.131: an empty body replays the cached request, and a Partial
    // request fills its unnamed collections from the same cache
    private async Task<Resolved> ResolveAsync(string userId, Sync? request, CancellationToken ct)
    {
        if (request is not null && !request.Partial)
        {
            // 2.2.3.131: a request has to carry at least Partial or Collections
            if (request.Collections is null)
                return new Resolved(null, 4);

            return new Resolved(new Effective(request, request.Collections.Items, FromCache: false), 0);
        }

        var cached = await _cache.GetAsync(userId, EasContext.DeviceId, ct);

        if (request is null)
        {
            // replay is armed only while nothing has moved, so the cached keys are still the
            // client's current ones
            if (cached is not { Replayable: true, Collections.Count: > 0 })
                return new Resolved(null, 13);

            var replay = new Sync { Wait = cached.Wait, HeartbeatInterval = cached.HeartbeatInterval };
            return new Resolved(
                new Effective(replay, [.. cached.Collections.Select(SyncRequestCacheMapping.ToRequest)], FromCache: true), 0);
        }

        var merged = new List<SyncCollection>(request.Collections?.Items ?? []);
        var named = merged.Select(c => c.CollectionId).ToHashSet(StringComparer.Ordinal);
        foreach (var c in cached?.Collections ?? [])
            if (named.Add(c.CollectionId))
                merged.Add(SyncRequestCacheMapping.ToRequest(c));

        if (merged.Count == 0)
            return new Resolved(null, 13);

        return new Resolved(new Effective(request, merged, FromCache: false), 0);
    }

    private async Task ExecuteAsync(string userId, Effective effective, CancellationToken ct)
    {
        var request = effective.Request;

        // Wait and HeartbeatInterval are mutually exclusive
        if (request.Wait is not null && request.HeartbeatInterval is not null)
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 4 }, _logger);
            return;
        }

        int? heartbeatSeconds = null;
        if (request.HeartbeatInterval is int hb)
        {
            if (hb is < MinHeartbeatSeconds or > MaxHeartbeatSeconds)
            {
                await WriteWbxmlResponseAsync(
                    new Sync { Status = 14, Limit = Math.Clamp(hb, MinHeartbeatSeconds, MaxHeartbeatSeconds) }, _logger);
                return;
            }
            heartbeatSeconds = hb;
        }
        else if (request.Wait is int wait)
        {
            if (wait is < MinWaitMinutes or > MaxWaitMinutes)
            {
                await WriteWbxmlResponseAsync(
                    new Sync { Status = 14, Limit = Math.Clamp(wait, MinWaitMinutes, MaxWaitMinutes) }, _logger);
                return;
            }
            heartbeatSeconds = wait * 60;
        }

        if (effective.Collections.Count > MaxCollectionsPerRequest)
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 15 }, _logger);
            return;
        }

        // disarm before touching anything, so a failure part way through can never leave a cache
        // entry that claims the collections are still where the client left them
        if (!effective.FromCache)
            await _cache.DisarmAsync(userId, EasContext.DeviceId, ct);

        // stale folder tree (e.g. after a mailbox rebuild): tell the client to FolderSync and retry
        if (await _itemSync.ResolveStaleHierarchyAsync(userId, EasContext.DeviceId, effective.Collections, ct))
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 12 }, _logger);
            return;
        }

        var results = new List<SyncCollection>();
        foreach (var c in effective.Collections)
            results.Add(await _itemSync.SyncAsync(userId, EasContext.DeviceId, c, ct));

        var quiet = IsLogicallyEmpty(effective.Collections, results);

        if (quiet && heartbeatSeconds is not null)
        {
            var monitored = effective.Collections
                .Select(c => c.CollectionId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            var deadline = DateTimeOffset.UtcNow.AddSeconds(heartbeatSeconds.Value);
            var changed = await _monitor.WaitForChangesAsync(userId, EasContext.DeviceId, monitored, deadline, ct);

            if (ct.IsCancellationRequested)
                return; // client disconnected, no response to write

            results = [];
            foreach (var c in effective.Collections.Where(c => changed.Contains(c.CollectionId)))
                results.Add(await _itemSync.SyncAsync(userId, EasContext.DeviceId, c, ct));

            quiet = IsLogicallyEmpty(effective.Collections, results);
        }

        await UpdateCacheAsync(userId, effective, quiet, ct);

        if (quiet)
        {
            // MS-ASCMD 2.2.1.21.2: nothing to report is headers and no XML payload
            HttpContext.Response.ContentType = "application/vnd.ms-sync.wbxml";
            return;
        }

        await WriteWbxmlResponseAsync(
            new Sync { Collections = new SyncCollections { Items = results } }, _logger);
    }

    private Task UpdateCacheAsync(string userId, Effective effective, bool quiet, CancellationToken ct)
    {
        if (!effective.FromCache)
            return _cache.StoreAsync(userId, EasContext.DeviceId,
                SyncRequestCacheMapping.ToCached(effective.Request, effective.Collections, quiet), ct);

        // a replay that produced changes leaves the cached keys behind the client, so make it come
        // back with a real request rather than replay the same window forever
        return quiet ? Task.CompletedTask : _cache.DisarmAsync(userId, EasContext.DeviceId, ct);
    }

    // Exchange's IsLogicallyEmptyResponse: no key moved, nothing to say, no error to report
    private static bool IsLogicallyEmpty(IReadOnlyList<SyncCollection> request, IReadOnlyList<SyncCollection> results)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in request)
            keys[c.CollectionId] = c.SyncKey;

        foreach (var r in results)
        {
            if (r.Status != 1) return false;
            if (r.Commands is { } cmd && cmd.Add.Count + cmd.Change.Count + cmd.Delete.Count + cmd.SoftDelete.Count > 0)
                return false;
            if (r.Responses is not null || r.MoreAvailable) return false;
            if (!keys.TryGetValue(r.CollectionId, out var sent) || r.SyncKey != sent) return false;
        }

        return true;
    }
}
