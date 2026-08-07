using System.Runtime.CompilerServices;
using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using EasFolderType = ReLiveWP.Services.Exchange.Models.FolderType;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;

namespace ReLiveWP.Services.Exchange.Services;

public class FolderSyncService(
    MailboxStore.MailboxStoreClient mailbox,
    OrphanFolderTracker orphans,
    ILogger<FolderSyncService> logger)
{
    public const int StatusSuccess = 1;
    public const int StatusNameExistsOrSpecial = 2;
    public const int StatusSpecialFolder = 3;
    public const int StatusFolderNotFound = 4;
    public const int StatusParentNotFound = 5;
    public const int StatusInvalidSyncKey = 9;
    public const int StatusMalformed = 10;

    public const string HierarchyCollectionId = "0"; // SyncState.FolderHierarchyCollectionId
    public const string RootParentId = "0";

    // FolderCreate accepts user-created types only
    private static readonly HashSet<EasFolderType> CreatableFolderTypes =
    [
        EasFolderType.Generic, EasFolderType.Mail, EasFolderType.Calendar,
        EasFolderType.Contacts, EasFolderType.Task, EasFolderType.Journal, EasFolderType.Notes,
    ];

    public async Task<FolderSync> SyncAsync(string userId,
                                            string deviceId,
                                            string? clientSyncKey,
                                            IReadOnlySet<string>? requestedAnnotations = null,
                                            CancellationToken ct = default)
    {
        clientSyncKey ??= "0";

        var state = await GetHierarchyStateAsync(userId, deviceId, ct);

        if (clientSyncKey == "0")
            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);

        if (state is null || state.SyncKey != clientSyncKey)
        {
            logger.LogWarning("FolderSync invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, clientSyncKey, state?.SyncKey ?? "<none>");

            if (state is not null)
            {
                await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    CollectionId = HierarchyCollectionId,
                    SyncKey = "0",
                    Watermark = 0,
                }, cancellationToken: ct);
            }

            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);
        }

        return await IncrementalSyncAsync(userId, state, requestedAnnotations, ct);
    }

    public async Task<FolderCreateResponse> CreateAsync(string userId,
                                                        string deviceId,
                                                        FolderCreate? request,
                                                        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrEmpty(request.SyncKey) || !IsValidDisplayName(request.DisplayName))
            return new FolderCreateResponse { Status = StatusMalformed };

        // creating a default folder (Inbox, Contacts, ...) is malformed, not a name collision
        if (!CreatableFolderTypes.Contains(request.Type))
            return new FolderCreateResponse { Status = StatusMalformed };

        var state = await GetHierarchyStateAsync(userId, deviceId, ct);
        if (state is null || state.SyncKey != request.SyncKey)
        {
            logger.LogWarning("FolderCreate invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, request.SyncKey, state?.SyncKey ?? "<none>");
            return new FolderCreateResponse { Status = StatusInvalidSyncKey };
        }

        var parentId = string.IsNullOrEmpty(request.ParentId) ? RootParentId : request.ParentId;
        Folder? createParent = null;
        if (parentId != RootParentId)
        {
            createParent = await GetFolderOrNullAsync(userId, parentId, ct);
            if (createParent is null)
                return new FolderCreateResponse { Status = StatusParentNotFound };
        }

        // MS-ASCMD 2.2.1.3: FolderCreate "cannot be used to create ... a subfolder of a
        // recipient information cache"
        if (createParent is not null && createParent.Type == ProtoFolderType.RecipientInformationCache)
            return new FolderCreateResponse { Status = StatusSpecialFolder };

        if (await FindChildByNameAsync(userId, parentId, request.DisplayName!, null, ct) is not null)
            return new FolderCreateResponse { Status = StatusNameExistsOrSpecial };

        var folder = await mailbox.CreateFolderAsync(new CreateFolderRequest
        {
            UserId = userId,
            DisplayName = request.DisplayName,
            Type = (ProtoFolderType)request.Type,
            ParentServerId = parentId,
        }, cancellationToken: ct);

        logger.LogInformation("FolderCreate {ServerId} ({Name}, type {Type}) under {Parent} for {User}",
            folder.Id, folder.DisplayName, folder.Type, parentId, userId);

        var newKey = await AdvanceHierarchyKeyAsync(userId, state, ct);
        return new FolderCreateResponse { Status = StatusSuccess, SyncKey = newKey, ServerId = folder.Id };
    }

    public async Task<FolderUpdateResponse> UpdateAsync(string userId,
                                                        string deviceId,
                                                        FolderUpdate? request,
                                                        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrEmpty(request.SyncKey) ||
            string.IsNullOrEmpty(request.ServerId) || !IsValidDisplayName(request.DisplayName))
            return new FolderUpdateResponse { Status = StatusMalformed };

        var state = await GetHierarchyStateAsync(userId, deviceId, ct);
        if (state is null || state.SyncKey != request.SyncKey)
        {
            logger.LogWarning("FolderUpdate invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, request.SyncKey, state?.SyncKey ?? "<none>");
            return new FolderUpdateResponse { Status = StatusInvalidSyncKey };
        }

        var folder = await GetFolderOrNullAsync(userId, request.ServerId, ct);
        if (folder is null)
            return new FolderUpdateResponse { Status = StatusFolderNotFound };

        if (IsSpecialFolderType(folder.Type))
            return new FolderUpdateResponse { Status = StatusNameExistsOrSpecial };

        var parentId = string.IsNullOrEmpty(request.ParentId) ? RootParentId : request.ParentId;
        Folder? newParent = null;
        if (parentId != RootParentId)
        {
            newParent = await GetFolderOrNullAsync(userId, parentId, ct);
            if (newParent is null)
                return new FolderUpdateResponse { Status = StatusParentNotFound };
        }

        // MS-ASCMD 2.2.1.6: FolderUpdate "cannot be used to move a folder under the recipient
        // information cache", and must return Status 3 if asked to
        if (newParent is not null && newParent.Type == ProtoFolderType.RecipientInformationCache)
            return new FolderUpdateResponse { Status = StatusSpecialFolder };

        // reparenting a folder beneath itself would detach the whole subtree from the root
        if (parentId != RootParentId && await WouldCreateCycleAsync(userId, folder.Id, parentId, ct))
            return new FolderUpdateResponse { Status = StatusParentNotFound };

        if (await FindChildByNameAsync(userId, parentId, request.DisplayName!, folder.Id, ct) is not null)
            return new FolderUpdateResponse { Status = StatusNameExistsOrSpecial };

        // UpdateFolder is a full replace: carry over everything unchanged or the folder loses its type/annotations
        var update = new UpdateFolderRequest
        {
            UserId = userId,
            ServerId = folder.Id,
            DisplayName = request.DisplayName,
            ParentServerId = parentId,
            Type = folder.Type,
            IsHidden = folder.IsHidden,
        };
        if (folder.HasSourceId) update.SourceId = folder.SourceId;
        if (folder.HasAccountName) update.AccountName = folder.AccountName;

        var result = await mailbox.UpdateFolderAsync(update, cancellationToken: ct);
        if (!result.Found)
            return new FolderUpdateResponse { Status = StatusFolderNotFound };

        logger.LogInformation("FolderUpdate {ServerId} -> ({Name}) under {Parent} for {User}",
            folder.Id, request.DisplayName, parentId, userId);

        var newKey = await AdvanceHierarchyKeyAsync(userId, state, ct);
        return new FolderUpdateResponse { Status = StatusSuccess, SyncKey = newKey };
    }

    public async Task<FolderDeleteResponse> DeleteAsync(string userId,
                                                        string deviceId,
                                                        FolderDelete? request,
                                                        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrEmpty(request.SyncKey) || string.IsNullOrEmpty(request.ServerId))
            return new FolderDeleteResponse { Status = StatusMalformed };

        var state = await GetHierarchyStateAsync(userId, deviceId, ct);
        if (state is null || state.SyncKey != request.SyncKey)
        {
            logger.LogWarning("FolderDelete invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, request.SyncKey, state?.SyncKey ?? "<none>");
            return new FolderDeleteResponse { Status = StatusInvalidSyncKey };
        }

        var folder = await GetFolderOrNullAsync(userId, request.ServerId, ct);
        if (folder is null)
            return new FolderDeleteResponse { Status = StatusFolderNotFound };

        if (IsSpecialFolderType(folder.Type))
            return new FolderDeleteResponse { Status = StatusSpecialFolder };

        // EmptyFolder walks the subtree; must run before DeleteFolder or children/items are orphaned
        await mailbox.EmptyFolderAsync(new EmptyFolderRequest
        {
            UserId = userId,
            CollectionId = folder.Id,
            DeleteSubFolders = true,
        }, cancellationToken: ct);

        var result = await mailbox.DeleteFolderAsync(new DeleteFolderRequest
        {
            UserId = userId,
            ServerId = folder.Id,
        }, cancellationToken: ct);

        if (!result.Found)
            return new FolderDeleteResponse { Status = StatusFolderNotFound };

        logger.LogInformation("FolderDelete {ServerId} ({Name}) for {User}", folder.Id, folder.DisplayName, userId);

        var newKey = await AdvanceHierarchyKeyAsync(userId, state, ct);
        return new FolderDeleteResponse { Status = StatusSuccess, SyncKey = newKey };
    }

    private async Task<SyncState?> GetHierarchyStateAsync(string userId, string deviceId, CancellationToken ct)
    {
        try
        {
            return await mailbox.GetSyncStateAsync(
                new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = HierarchyCollectionId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    // bumps the SyncKey but deliberately leaves the watermark alone: snapping it to the event tip
    // would swallow changes the device hasn't seen yet, a redundant replay next FolderSync is harmless
    private async Task<string> AdvanceHierarchyKeyAsync(string userId, SyncState state, CancellationToken ct)
    {
        var newKey = SyncEngine.NextSyncKey(state.SyncKey);
        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = state.DeviceId,
            CollectionId = HierarchyCollectionId,
            SyncKey = newKey,
            Watermark = state.Watermark,
        }, cancellationToken: ct);
        return newKey;
    }

    private async Task<Folder?> GetFolderOrNullAsync(string userId, string serverId, CancellationToken ct)
    {
        try
        {
            return await mailbox.GetFolderAsync(
                new GetFolderRequest { UserId = userId, ServerId = serverId }, cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Folder?> FindChildByNameAsync(string userId, string parentId, string displayName,
                                                     string? excludeServerId, CancellationToken ct)
    {
        await foreach (var f in ReadFoldersAsync(userId, includeHidden: true, ct))
            if (f.ParentServerId == parentId &&
                f.Id != excludeServerId &&
                string.Equals(f.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                return f;

        return null;
    }

    private async Task<bool> WouldCreateCycleAsync(string userId, string serverId, string newParentId,
                                                   CancellationToken ct)
    {
        var parents = new Dictionary<string, string>(StringComparer.Ordinal);
        await foreach (var f in ReadFoldersAsync(userId, includeHidden: true, ct))
            parents[f.Id] = f.ParentServerId;

        for (var id = newParentId; id != RootParentId && parents.TryGetValue(id, out var parent);)
        {
            if (id == serverId)
                return true;
            id = parent;
        }

        return false;
    }

    private static bool IsValidDisplayName(string? name) =>
        !string.IsNullOrEmpty(name) && name.Length <= 256;

    private static bool IsSpecialFolderType(ProtoFolderType t) => t is
        ProtoFolderType.InboxDefault or ProtoFolderType.DraftsDefault or ProtoFolderType.DeletedItemsDefault or
        ProtoFolderType.SentItemsDefault or ProtoFolderType.OutboxDefault or ProtoFolderType.TasksDefault or
        ProtoFolderType.CalendarDefault or ProtoFolderType.ContactsDefault or ProtoFolderType.NotesDefault or
        ProtoFolderType.JournalDefault or ProtoFolderType.RecipientInformationCache or ProtoFolderType.MeContact;

    private static bool AbchAnnotationsRequested(IReadOnlySet<string>? requested) =>
        requested is not null &&
        (requested.Contains("SID") || requested.Contains("AN") || requested.Contains("DomainId"));

    private async Task<FolderSync> InitialSyncAsync(string userId,
                                                    string deviceId,
                                                    SyncState? state,
                                                    IReadOnlySet<string>? requestedAnnotations,
                                                    CancellationToken ct)
    {
        var tip = (await mailbox.GetFolderEventTipAsync(
            new FolderEventTipRequest { UserId = userId }, cancellationToken: ct)).Value;

        bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);

        var changes = new Changes();
        await foreach (var f in ReadFoldersAsync(userId, includeHidden: abchRequested, ct))
            changes.Add.Add(ToFolderChange(f, requestedAnnotations));
        changes.Count = changes.Add.Count;

        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = deviceId,
            CollectionId = HierarchyCollectionId,
            SyncKey = "1",
            Watermark = tip,
        }, cancellationToken: ct);

        return new FolderSync { Status = StatusSuccess, SyncKey = "1", Changes = changes };
    }

    private async Task<FolderSync> IncrementalSyncAsync(string userId,
                                                        SyncState state,
                                                        IReadOnlySet<string>? requestedAnnotations,
                                                        CancellationToken ct)
    {
        // WP7 doesn't handle FolderSync Status 9 well, so fake a resync with explicit deletes instead
        var orphanIds = orphans.Drain(state.DeviceId);

        var events = new List<SyncEvent>();
        await foreach (var e in ReadFolderEventsAsync(userId, state.Watermark, ct))
            events.Add(new SyncEvent(e.CommitId, e.Id, e.ServerId, e.EventType));

        if (events.Count == 0 && orphanIds.Count == 0)
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

        var changes = new Changes();
        long watermark = state.Watermark;

        if (events.Count > 0)
        {
            var delta = SyncEngine.Collapse(events);
            watermark = delta.Watermark;

            bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);
            var wanted = delta.Added.Concat(delta.Updated).ToHashSet();
            var folders = new Dictionary<string, Folder>();
            await foreach (var f in ReadFoldersAsync(userId, abchRequested, ct))
                if (wanted.Contains(f.Id))
                    folders[f.Id] = f;

            changes.Add = [.. delta.Added.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations))];
            changes.Update = [.. delta.Updated.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations))];
            changes.Delete = [.. delta.Deleted.Select(id => new FolderChange { ServerId = id })];
        }

        foreach (var id in orphanIds)
            if (!changes.Delete.Any(d => d.ServerId == id))
                changes.Delete.Add(new FolderChange { ServerId = id });

        changes.Count = changes.Add.Count + changes.Update.Count + changes.Delete.Count;

        var newKey = SyncEngine.NextSyncKey(state.SyncKey);
        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = state.DeviceId,
            CollectionId = HierarchyCollectionId,
            SyncKey = newKey,
            Watermark = watermark,
        }, cancellationToken: ct);

        return new FolderSync { Status = StatusSuccess, SyncKey = newKey, Changes = changes };
    }

    private async IAsyncEnumerable<Folder> ReadFoldersAsync(string userId, bool includeHidden,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = mailbox.ListFolders(new ListFoldersRequest
        { UserId = userId, IncludeHidden = includeHidden, IncludeDeleted = false }, cancellationToken: ct);
        await foreach (var f in call.ResponseStream.ReadAllAsync(ct))
            yield return f;
    }

    private async IAsyncEnumerable<FolderEvent> ReadFolderEventsAsync(string userId, long afterWatermark,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = mailbox.GetFolderEvents(new GetFolderEventsRequest
        { UserId = userId, AfterWatermark = afterWatermark }, cancellationToken: ct);
        await foreach (var e in call.ResponseStream.ReadAllAsync(ct))
            yield return e;
    }

    private static FolderChange ToFolderChange(Folder f, IReadOnlySet<string>? requested) => new()
    {
        ServerId = f.Id,
        ParentId = f.ParentServerId,
        DisplayName = f.DisplayName,
        Type = ToEasFolderType(f.Type),
        Annotations = BuildFolderAnnotations(f, requested),
    };

    private static EasFolderType ToEasFolderType(ProtoFolderType t) 
        => (EasFolderType)t;

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
