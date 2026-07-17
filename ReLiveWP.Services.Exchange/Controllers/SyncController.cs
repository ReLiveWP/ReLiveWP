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

    public SyncController(ILogger<SyncController> logger, ItemSyncService itemSync, PushMonitor monitor)
    {
        _logger = logger;
        _itemSync = itemSync;
        _monitor = monitor;
    }

    [HttpPost]
    public async Task Post()
    {
        var ct = HttpContext.RequestAborted;

        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<Sync>(EasContext.XmlDocument)
            : null;

        // Wait and HeartbeatInterval are mutually exclusive
        if (request?.Wait is not null && request?.HeartbeatInterval is not null)
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 4 }, _logger);
            return;
        }

        int? heartbeatSeconds = null;
        if (request?.HeartbeatInterval is int hb)
        {
            if (hb is < MinHeartbeatSeconds or > MaxHeartbeatSeconds)
            {
                await WriteWbxmlResponseAsync(
                    new Sync { Status = 14, Limit = Math.Clamp(hb, MinHeartbeatSeconds, MaxHeartbeatSeconds) }, _logger);
                return;
            }
            heartbeatSeconds = hb;
        }
        else if (request?.Wait is int wait)
        {
            if (wait is < MinWaitMinutes or > MaxWaitMinutes)
            {
                await WriteWbxmlResponseAsync(
                    new Sync { Status = 14, Limit = Math.Clamp(wait, MinWaitMinutes, MaxWaitMinutes) }, _logger);
                return;
            }
            heartbeatSeconds = wait * 60;
        }

        var userId = User.Id()!;

        try
        {
            if (request?.Collections is not null)
            {
                await HandleCollectionsAsync(userId, request, heartbeatSeconds, ct);
                return;
            }

            // empty/partial Sync: sync every collection the device already has, not a no-op. A bare
            // <Sync/> reply makes WP8 reject it and retry in a tight Settings-FolderSync-Sync loop.
            await HandlePartialAsync(userId, heartbeatSeconds, ct);
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

    private async Task HandleCollectionsAsync(string userId, Sync request, int? heartbeatSeconds, CancellationToken ct)
    {
        if (request.Collections!.Items.Count > MaxCollectionsPerRequest)
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 15 }, _logger);
            return;
        }

        // stale folder tree (e.g. after a mailbox rebuild): tell the client to FolderSync and retry
        if (await _itemSync.ResolveStaleHierarchyAsync(userId, EasContext.DeviceId, request.Collections!.Items, ct))
        {
            await WriteWbxmlResponseAsync(new Sync { Status = 12 }, _logger);
            return;
        }

        var collections = new List<SyncCollection>();
        var anyContent = false;
        foreach (var c in request.Collections.Items)
        {
            var result = await _itemSync.SyncAsync(userId, EasContext.DeviceId, c, ct);
            collections.Add(result);
            anyContent |= HasContent(result);
        }

        if (anyContent || heartbeatSeconds is null)
        {
            await WriteWbxmlResponseAsync(
                new Sync { Collections = new SyncCollections { Items = collections } }, _logger);
            return;
        }

        var monitored = request.Collections.Items
            .Select(c => c.CollectionId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet();

        await HoldAndRespondAsync(userId, monitored, heartbeatSeconds.Value, ct);
    }

    private async Task HandlePartialAsync(string userId, int? heartbeatSeconds, CancellationToken ct)
    {
        var deviceCollections = await _itemSync.ListDeviceCollectionIdsAsync(userId, EasContext.DeviceId, ct);
        if (deviceCollections.Count == 0)
        {
            // no cached collection set, ask the client to resend a full Sync
            await WriteWbxmlResponseAsync(new Sync { Status = 13 }, _logger);
            return;
        }

        var results = new List<SyncCollection>();
        var anyContent = false;
        foreach (var cid in deviceCollections)
        {
            var result = await _itemSync.SyncByCollectionIdAsync(userId, EasContext.DeviceId, cid, ct);
            if (result is null) continue;
            results.Add(result);
            anyContent |= HasContent(result);
        }

        if (anyContent || heartbeatSeconds is null)
        {
            await WriteWbxmlResponseAsync(
                new Sync { Collections = new SyncCollections { Items = results } }, _logger);
            return;
        }

        await HoldAndRespondAsync(userId, deviceCollections.ToHashSet(), heartbeatSeconds.Value, ct);
    }

    private async Task HoldAndRespondAsync(string userId, HashSet<string> monitored, int heartbeatSeconds, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(heartbeatSeconds);
        var changed = await _monitor.WaitForChangesAsync(userId, EasContext.DeviceId, monitored, deadline, ct);

        if (ct.IsCancellationRequested)
            return; // client disconnected, no response to write

        if (changed.Count == 0)
        {
            // heartbeat expired with no changes: empty 200 body so the client re-issues
            HttpContext.Response.ContentType = "application/vnd.ms-sync.wbxml";
            return;
        }

        var collections = new List<SyncCollection>();
        foreach (var cid in changed)
        {
            var result = await _itemSync.SyncByCollectionIdAsync(userId, EasContext.DeviceId, cid, ct);
            if (result is not null) collections.Add(result);
        }

        await WriteWbxmlResponseAsync(
            new Sync { Collections = new SyncCollections { Items = collections } }, _logger);
    }

    private static bool HasContent(SyncCollection c) =>
        (c.Commands is { } cmd && cmd.Add.Count + cmd.Change.Count + cmd.Delete.Count + cmd.SoftDelete.Count > 0)
        || c.Responses is not null
        || c.MoreAvailable;
}
