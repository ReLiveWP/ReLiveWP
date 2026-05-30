using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using EasFolderType = ReLiveWP.Services.Exchange.Models.FolderType;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;

namespace ReLiveWP.Services.Exchange.Services;

public class FolderSyncService(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<FolderSyncService> logger)
{
    public const int StatusSuccess = 1;
    public const int StatusInvalidSyncKey = 9;

    private const string HierarchyCollectionId = "0"; // SyncState.FolderHierarchyCollectionId

    public async Task<FolderSync> SyncAsync(string userId, string deviceId, string? clientSyncKey,
        IReadOnlySet<string>? requestedAnnotations = null, CancellationToken ct = default)
    {
        clientSyncKey ??= "0";

        SyncState? state;
        try
        {
            var s = await mailbox.GetSyncStateAsync(
                new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = HierarchyCollectionId },
                cancellationToken: ct);
            state = s;
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            state = null;
        }

        if (clientSyncKey == "0")
            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);

        if (state is null || state.SyncKey != clientSyncKey)
        {
            logger.LogWarning("FolderSync invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, clientSyncKey, state?.SyncKey ?? "<none>");

            if (state is not null)
                await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    CollectionId = HierarchyCollectionId,
                    SyncKey = "0",
                    Watermark = 0,
                }, cancellationToken: ct);

            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);
        }

        return await IncrementalSyncAsync(userId, state, requestedAnnotations, ct);
    }

    private static bool AbchAnnotationsRequested(IReadOnlySet<string>? requested) =>
        requested is not null &&
        (requested.Contains("SID") || requested.Contains("AN") || requested.Contains("DomainId"));

    private async Task<FolderSync> InitialSyncAsync(string userId, string deviceId,
        SyncState? state, IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        var tip = (await mailbox.GetFolderEventTipAsync(
            new FolderEventTipRequest { UserId = userId }, cancellationToken: ct)).Value;

        bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);
        var folders = await ReadAllFoldersAsync(userId, includeHidden: abchRequested, ct);

        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = deviceId,
            CollectionId = HierarchyCollectionId,
            SyncKey = "1",
            Watermark = tip,
        }, cancellationToken: ct);

        var changes = new Changes { Add = [.. folders.Select(f => ToFolderChange(f, requestedAnnotations))] };
        changes.Count = changes.Add.Count;

        return new FolderSync { Status = StatusSuccess, SyncKey = "1", Changes = changes };
    }

    private async Task<FolderSync> IncrementalSyncAsync(string userId, SyncState state,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        var events = await ReadAllFolderEventsAsync(userId, state.Watermark, ct);

        if (events.Count == 0)
        {
            await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = state.DeviceId,
                CollectionId = HierarchyCollectionId,
                SyncKey = state.SyncKey,
                Watermark = state.Watermark,
            }, cancellationToken: ct);
            return new FolderSync { Status = StatusSuccess, SyncKey = state.SyncKey, Changes = new Changes() };
        }

        var delta = SyncEngine.Collapse(events.Select(e => new SyncEvent(e.Id, e.ServerId, e.EventType)).ToList());

        bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);
        var ids = delta.Added.Concat(delta.Updated).ToList();
        var folders = (await ReadFoldersByIdsAsync(userId, ids, abchRequested, ct))
            .ToDictionary(f => f.Id);

        var changes = new Changes
        {
            Add = [.. delta.Added.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations))],
            Update = [.. delta.Updated.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations))],
            Delete = [.. delta.Deleted.Select(id => new FolderChange { ServerId = id })],
        };
        changes.Count = changes.Add.Count + changes.Update.Count + changes.Delete.Count;

        var newKey = SyncEngine.NextSyncKey(state.SyncKey);
        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = state.DeviceId,
            CollectionId = HierarchyCollectionId,
            SyncKey = newKey,
            Watermark = delta.Watermark,
        }, cancellationToken: ct);

        return new FolderSync { Status = StatusSuccess, SyncKey = newKey, Changes = changes };
    }

    // ── gRPC stream helpers ───────────────────────────────────────────────────

    private async Task<List<Folder>> ReadAllFoldersAsync(string userId, bool includeHidden, CancellationToken ct)
    {
        var result = new List<Folder>();
        using var call = mailbox.ListFolders(new ListFoldersRequest
        { UserId = userId, IncludeHidden = includeHidden, IncludeDeleted = false });
        await foreach (var f in call.ResponseStream.ReadAllAsync(ct))
            result.Add(f);
        return result;
    }

    private async Task<List<Folder>> ReadFoldersByIdsAsync(string userId, List<string> ids,
        bool includeHidden, CancellationToken ct)
    {
        var all = await ReadAllFoldersAsync(userId, includeHidden, ct);
        var idSet = ids.ToHashSet();
        return [.. all.Where(f => idSet.Contains(f.Id))];
    }

    private async Task<List<FolderEvent>> ReadAllFolderEventsAsync(string userId, long afterWatermark, CancellationToken ct)
    {
        var result = new List<FolderEvent>();
        using var call = mailbox.GetFolderEvents(new GetFolderEventsRequest
        { UserId = userId, AfterWatermark = afterWatermark });
        await foreach (var e in call.ResponseStream.ReadAllAsync(ct))
            result.Add(e);
        return result;
    }

    // ── EAS XML mapping ───────────────────────────────────────────────────────

    private static FolderChange ToFolderChange(Folder f, IReadOnlySet<string>? requested) => new()
    {
        ServerId = f.Id,
        ParentId = f.ParentServerId,
        DisplayName = f.DisplayName,
        Type = ToEasFolderType(f.Type),
        Annotations = BuildFolderAnnotations(f, requested),
    };

    private static EasFolderType ToEasFolderType(ProtoFolderType t) => (EasFolderType)(int)t;

    private static Annotations? BuildFolderAnnotations(Folder f, IReadOnlySet<string>? requested)
    {
        if (requested is null || requested.Count == 0 || !f.HasSourceId)
            return null;

        var items = new List<Annotation>();

        void Add(string name, string? value)
        {
            if (requested.Contains(name) && value is not null)
                items.Add(new Annotation { Name = name, Value = value });
        }

        Add("SID", f.SourceId);
        Add("AN", f.HasAccountName ? f.AccountName : null);

        if (requested.Contains("DomainId") && KnownDomainIds.TryGetValue(f.SourceId, out var domainId))
            items.Add(new Annotation { Name = "DomainId", Value = domainId.ToString() });

        return items.Count > 0 ? new Annotations { Items = items } : null;
    }

    private static readonly Dictionary<string, int> KnownDomainIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ABCH"] = 18,
        ["FB"] = 7,
        ["LI"] = 8,
        ["GOOG"] = 20,
        ["YHOO"] = 21,
        ["TWITR"] = 22,
        ["SKYPE"] = 129,
    };
}
