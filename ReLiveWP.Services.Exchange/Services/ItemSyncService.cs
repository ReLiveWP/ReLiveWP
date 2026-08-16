using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;
using ReLiveWP.Services.Exchange.Extensions;

namespace ReLiveWP.Services.Exchange.Services;

public class ItemSyncService(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<ItemSyncService> logger,
    IOptions<EasSyncOptions> options,
    MeProfileWriteback? profileWriteback = null)
{
    private static readonly IReadOnlySet<string> EmptyServerIds = new HashSet<string>();

    // read once per scope: a monitor would let the flag flip between two collections of one request
    private readonly bool absentSupportedClearsOmitted = options.Value.AbsentSupportedClearsOmitted;

    // bounded so a genuinely contended item still resolves to a conflict rather than spinning
    private const int MaxChangeWriteAttempts = 3;

    // if we can't find a collection, the device is probably out of sync, reset it
    public async Task<bool> ResolveStaleHierarchyAsync(string userId,
                                                       string deviceId,
                                                       IEnumerable<SyncCollection> collections,
                                                       CancellationToken ct = default)
    {
        bool stale = false;
        foreach (var c in collections)
        {
            if (string.IsNullOrEmpty(c.CollectionId)) continue;
            try
            {
                await mailbox.GetFolderAsync(
                    new GetFolderRequest { UserId = userId, ServerId = c.CollectionId },
                    cancellationToken: ct);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                stale = true;
                break;
            }
        }

        if (stale)
            await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = deviceId,
                CollectionId = FolderSyncService.HierarchyCollectionId, // "0"
                SyncKey = "0",
                Watermark = 0,
            }, cancellationToken: ct);

        return stale;
    }

    public async Task<SyncCollection> SyncAsync(string userId,
                                                string deviceId,
                                                SyncCollection request,
                                                CancellationToken ct = default)
    {
        var collectionId = request.CollectionId;
        var hasClientCommands = request.Commands is { } reqCmds &&
            (reqCmds.Add.Count > 0 || reqCmds.Change.Count > 0 || reqCmds.Delete.Count > 0 || reqCmds.Fetch.Count > 0);
        var getChanges = request.GetChanges ?? (request.SyncKey != "0");

        SyncState? state;
        try
        {
            state = await mailbox.GetSyncStateAsync(
                new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = collectionId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            state = null;
        }

        SyncCollection result;
        IReadOnlySet<string> serverChangedIds;
        GhostingPolicy ghosting = GhostingPolicy.Parse(state?.SupportedElements)
                                                .Effective(absentSupportedClearsOmitted);
        var windowSize = SyncEngine.ResolveWindowSize(request.WindowSize);
        var windowSizeSent = request.WindowSize.HasValue;

        if (request.SyncKey == "0")
        {
            var annotationNames = request.Options?.Annotations?.RequestedNames();
            var declared = GhostingPolicy.FromSupported(request.Supported);

            (result, serverChangedIds) = (await InitialSyncAsync(userId, deviceId, collectionId, state, annotationNames, declared, ct), EmptyServerIds);
            
            ghosting = declared.Effective(absentSupportedClearsOmitted);
        }
        else if (state is not null && request.SyncKey == state.PreviousSyncKey && request.SyncKey != state.SyncKey)
        {
            state = new SyncState
            {
                UserId = userId,
                DeviceId = deviceId,
                CollectionId = collectionId,
                SyncKey = request.SyncKey,
                Watermark = state.PreviousWatermark,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
                SupportedElements = state.SupportedElements ?? string.Empty,
                PreviousSyncKey = state.PreviousSyncKey,
                PreviousWatermark = state.PreviousWatermark,
            };

            var annotationNames = ParseCachedAnnotationNames(state.CachedAnnotationNames);
            var bodyPrefs = SelectBodyPreference(request.Options);
            (result, serverChangedIds) = await IncrementalSyncAsync(userId, collectionId, state, getChanges, hasClientCommands, annotationNames, bodyPrefs, windowSize, windowSizeSent, ct);
        }
        else if (state is null || state.SyncKey != request.SyncKey)
        {
            if (state is not null)
            {
                await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    CollectionId = collectionId,
                    SyncKey = "0",
                    Watermark = 0,
                    CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
                    SupportedElements = state.SupportedElements ?? string.Empty,
                    PreviousSyncKey = "0",
                    PreviousWatermark = 0,
                }, cancellationToken: ct);
            }

            return new SyncCollection { CollectionId = collectionId, SyncKey = "0", Status = 3 };
        }
        else
        {
            var annotationNames = ParseCachedAnnotationNames(state.CachedAnnotationNames);
            var bodyPrefs = SelectBodyPreference(request.Options);
            (result, serverChangedIds) = await IncrementalSyncAsync(userId, collectionId, state, getChanges, hasClientCommands, annotationNames, bodyPrefs, windowSize, windowSizeSent, ct);
        }

        if (request.Commands is { } cmds &&
            (cmds.Add.Count > 0 || cmds.Change.Count > 0 || cmds.Delete.Count > 0 || cmds.Fetch.Count > 0))
        {
            var itemClass = await GetItemClassAsync(userId, collectionId, ct);
            var bodyPrefs = SelectBodyPreference(request.Options);
            var conflictPolicy = request.Options?.Conflict ?? SyncConflict.ServerWins;
            var responses = await ProcessClientCommandsAsync(userId, collectionId, itemClass, cmds, bodyPrefs, serverChangedIds, conflictPolicy, ghosting, ct);
            if (responses is not null)
                result.Responses = responses;

            SyncEngine.SuppressDeleted(result.Commands, cmds.Delete.Select(d => d.ServerId));
            if (result.Commands is { } rc &&
                rc.Add.Count + rc.Change.Count + rc.Delete.Count + rc.SoftDelete.Count + rc.Fetch.Count == 0)
                result.Commands = null;
        }

        return result;
    }

    private async Task<SyncCollection> InitialSyncAsync(string userId,
                                                        string deviceId,
                                                        string collectionId,
                                                        SyncState? state,
                                                        IReadOnlySet<string>? requestedAnnotations,
                                                        GhostingPolicy declared,
                                                        CancellationToken ct)
    {
        var cached = requestedAnnotations is { Count: > 0 }
            ? string.Join(",", requestedAnnotations)
            : string.IsNullOrEmpty(state?.CachedAnnotationNames) ? null : state.CachedAnnotationNames;

        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = deviceId,
            CollectionId = collectionId,
            SyncKey = "1",
            Watermark = -1,
            CachedAnnotationNames = cached ?? string.Empty,
            SupportedElements = declared.Serialize(),
            PreviousSyncKey = "0",
            PreviousWatermark = 0,
        }, cancellationToken: ct);

        return new SyncCollection { CollectionId = collectionId, SyncKey = "1", Status = 1 };
    }

    private async Task<(SyncCollection Collection, IReadOnlySet<string> ServerChangedIds)> IncrementalSyncAsync(
        string userId,
        string collectionId,
        SyncState state,
        bool getChanges,
        bool hasClientCommands,
        IReadOnlySet<string>? requestedAnnotations,
        IReadOnlyList<BodyPreference>? bodyPrefs,
        int windowSize,
        bool windowSizeSent,
        CancellationToken ct)
    {
        Task UpsertAdvancedAsync(string newKey, long newWatermark)
        {
            bool advanced = newKey != state.SyncKey;
            return mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = state.DeviceId,
                CollectionId = collectionId,
                SyncKey = newKey,
                Watermark = newWatermark,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
                SupportedElements = state.SupportedElements ?? string.Empty,
                PreviousSyncKey = advanced ? state.SyncKey : state.PreviousSyncKey,
                PreviousWatermark = advanced ? state.Watermark : state.PreviousWatermark,
            }, cancellationToken: ct).ResponseAsync;
        }

        // -1 means "never synced": treat as watermark 0 so the very first real sync flows through
        // the same ItemEvent-log-driven path (and windowing) as every incremental sync
        long baseline = state.Watermark == -1 ? 0 : state.Watermark;

        if (!getChanges)
        {
            var noContentEvents = new List<SyncEvent>();
            await foreach (var e in ReadItemEventsAsync(userId, collectionId, baseline, ct))
                noContentEvents.Add(new SyncEvent(e.CommitId, e.Id, e.ServerId, e.EventType));
            var noContentDelta = SyncEngine.Collapse(noContentEvents);

            var key = hasClientCommands ? SyncEngine.NextSyncKey(state.SyncKey) : state.SyncKey;
            await UpsertAdvancedAsync(key, baseline);
            return (new SyncCollection { CollectionId = collectionId, SyncKey = key, Status = 1 }, noContentDelta.AllUpdatedServerIds);
        }

        var events = new List<SyncEvent>();
        await foreach (var e in ReadItemEventsAsync(userId, collectionId, baseline, ct))
            events.Add(new SyncEvent(e.CommitId, e.Id, e.ServerId, e.EventType));

        if (events.Count == 0)
        {
            // settles on the baseline rather than writing -1 back: an empty collection that keeps
            // its "never synced" marker is reported as changed by every Ping, forever
            var key = hasClientCommands ? SyncEngine.NextSyncKey(state.SyncKey) : state.SyncKey;
            await UpsertAdvancedAsync(key, baseline);
            return (new SyncCollection { CollectionId = collectionId, SyncKey = key, Status = 1 }, EmptyServerIds);
        }

        var delta = SyncEngine.Collapse(events, windowSize);
        var ids = delta.Added.Concat(delta.Updated).ToList();

        var itemMap = new Dictionary<string, Item>();
        await foreach (var i in ReadItemsByIdsAsync(userId, ids, ct))
            itemMap[i.ServerId] = i;

        var cmds = new SyncCommands
        {
            Delete = [.. delta.Deleted.Select(id => new SyncItemRef { ServerId = id })],
        };

        // a bad row is skipped, not thrown, so it doesn't cost the device the whole batch
        foreach (var id in delta.Added)
        {
            if (!itemMap.TryGetValue(id, out var item)) continue;
            var data = TrySerialize(item, requestedAnnotations, bodyPrefs);
            if (data is not null) cmds.Add.Add(new SyncAdd { ServerId = id, ApplicationData = data });
        }
        foreach (var id in delta.Updated)
        {
            if (!itemMap.TryGetValue(id, out var item)) continue;
            var data = TrySerialize(item, requestedAnnotations, bodyPrefs);
            if (data is not null) cmds.Change.Add(new SyncChange { ServerId = id, ApplicationData = data });
        }

        var nextKey = SyncEngine.NextSyncKey(state.SyncKey);
        await UpsertAdvancedAsync(nextKey, delta.Watermark);

        bool hasChanges = cmds.Add.Count + cmds.Change.Count + cmds.Delete.Count > 0;
        return (new SyncCollection
        {
            CollectionId = collectionId,
            SyncKey = nextKey,
            Status = 1,
            Commands = hasChanges ? cmds : null,
            // MoreAvailable is only ever advertised in response to a client-supplied WindowSize
            MoreAvailable = delta.MoreAvailable && windowSizeSent,
        }, delta.AllUpdatedServerIds);
    }

    private async Task<SyncResponses?> ProcessClientCommandsAsync(string userId,
                                                                  string collectionId,
                                                                  string itemClass,
                                                                  SyncCommands cmds,
                                                                  IReadOnlyList<BodyPreference>? bodyPrefs,
                                                                  IReadOnlySet<string> serverChangedIds,
                                                                  SyncConflict conflictPolicy,
                                                                  GhostingPolicy ghosting,
                                                                  CancellationToken ct)
    {
        var responses = new SyncResponses();

        foreach (var add in cmds.Add)
        {
            var (serverId, status) = await HandleAddAsync(userId, collectionId, itemClass, add.ClientId, add.ApplicationData, ct);
            responses.Add.Add(new SyncResponseAdd
            {
                ClientId = add.ClientId ?? string.Empty,
                ServerId = serverId,
                Status = status,
            });
        }

        foreach (var change in cmds.Change)
        {
            int status = await HandleChangeAsync(userId, itemClass, change, serverChangedIds, conflictPolicy, ghosting, ct);
            if (status != 1)
                responses.Change.Add(new SyncResponseChange { ServerId = change.ServerId, Status = status });
        }

        foreach (var delete in cmds.Delete)
        {
            var result = await mailbox.DeleteItemAsync(
                new DeleteItemRequest { UserId = userId, ServerId = delete.ServerId },
                cancellationToken: ct);
            if (!result.Found)
                responses.Delete.Add(new SyncResponseDelete { ServerId = delete.ServerId, Status = 8 });
        }

        foreach (var fetch in cmds.Fetch)
            responses.Fetch.Add(await HandleFetchAsync(userId, fetch.ServerId, bodyPrefs, ct));

        bool any = responses.Add.Count + responses.Change.Count + responses.Delete.Count + responses.Fetch.Count > 0;
        return any ? responses : null;
    }

    // some devices fetch message bodies with an embedded fetch
    private async Task<SyncResponseFetch> HandleFetchAsync(string userId,
                                                           string serverId,
                                                           IReadOnlyList<BodyPreference>? bodyPrefs,
                                                           CancellationToken ct)
    {
        Item item;
        try
        {
            item = await mailbox.GetItemAsync(
                new GetItemRequest { UserId = userId, ServerId = serverId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return new SyncResponseFetch { ServerId = serverId, Status = 8 }; // object not found
        }

        var appData = TrySerialize(item, requestedAnnotations: null, bodyPrefs);
        if (appData is null)
            return new SyncResponseFetch { ServerId = serverId, Status = 8 };

        return new SyncResponseFetch { ServerId = serverId, Status = 1, ApplicationData = appData };
    }

    private async Task<(string? serverId, int status)> HandleAddAsync(string userId,
                                                                      string collectionId,
                                                                      string itemClass,
                                                                      string? clientId,
                                                                      ApplicationData? appData,
                                                                      CancellationToken ct)
    {
        CreateItemRequest? req;
        try
        {
            req = BuildCreateRequest(userId, collectionId, itemClass, appData);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Sync Add: unparseable {ItemClass} content in {Collection}", itemClass, collectionId);
            return (null, 6);
        }

        if (req is null) return (null, 6);
        // client id lets the store dedupe a retransmitted add
        if (!string.IsNullOrEmpty(clientId)) req.ClientId = clientId;

        try
        {
            var item = await mailbox.CreateItemAsync(req, cancellationToken: ct);
            return (item.ServerId, 1);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
        {
            // status 6 is scoped to this item, so the rest of the batch still commits
            logger.LogWarning("Sync Add rejected in {Collection}: {Detail}", collectionId, e.Status.Detail);
            return (null, 6);
        }
    }

    internal async Task<int> HandleChangeAsync(string userId,
                                              string itemClass,
                                              SyncChange change,
                                              IReadOnlySet<string> serverChangedIds,
                                              SyncConflict conflictPolicy,
                                              GhostingPolicy ghosting,
                                              CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            Item existing;
            try
            {
                existing = await mailbox.GetItemAsync(
                    new GetItemRequest { UserId = userId, ServerId = change.ServerId },
                    cancellationToken: ct);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                return 8;
            }
            
            if (conflictPolicy == SyncConflict.ServerWins && serverChangedIds.Contains(change.ServerId))
            {
                logger.LogInformation(
                    "Sync Change conflict for {ServerId}: server-wins, discarding client change", change.ServerId);
                return 7;
            }

            UpdateItemRequest? req;
            try
            {
                req = BuildUpdateRequest(userId, itemClass, change.ServerId, change.ApplicationData, existing, ghosting);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "Sync Change: unparseable {ItemClass} content for {ServerId}", itemClass, change.ServerId);
                return 6;
            }

            if (req is null) return 8;
            req.ExpectedVersion = existing.Version;

            try
            {
                var result = await mailbox.UpdateItemAsync(req, cancellationToken: ct);
                if (!result.Found) return 8;
                if (!result.Conflict)
                {
                    if (profileWriteback is not null && itemClass == "Contact" && IsSelfContact(existing.Contact))
                        await profileWriteback.WriteBackAsync(userId, existing.Contact, req.Contact, ct);

                    return 1;
                }
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                logger.LogWarning("Sync Change rejected for {ServerId}: {Detail}", change.ServerId, e.Status.Detail);
                return 6;
            }

            if (attempt >= MaxChangeWriteAttempts)
            {
                logger.LogWarning(
                    "Sync Change for {ServerId} lost the write race {Attempts} times, reporting conflict",
                    change.ServerId, attempt);
                return 7;
            }
        }
    }

    private static CreateItemRequest? BuildCreateRequest(string userId,
                                                         string collectionId,
                                                         string itemClass,
                                                         ApplicationData? appData) =>
        itemClass switch
        {
            "Contact" when DeserializeAppData<ContactData>(appData) is { } cd =>
                new CreateItemRequest
                {
                    UserId = userId,
                    CollectionId = collectionId,
                    Contact = cd.ToProtoContact()
                },
            "Calendar" when DeserializeAppData<CalendarData>(appData) is { } cal =>
                new CreateItemRequest
                {
                    UserId = userId,
                    CollectionId = collectionId,
                    Calendar = cal.ToProtoCalendar()
                },
            "Task" when DeserializeAppData<TaskData>(appData) is { } td =>
                new CreateItemRequest
                {
                    UserId = userId,
                    CollectionId = collectionId,
                    Task = td.ToProtoTask()
                },
            "Note" when DeserializeAppData<NoteData>(appData) is { } nd =>
                new CreateItemRequest
                {
                    UserId = userId,
                    CollectionId = collectionId,
                    Note = nd.ToProtoNote()
                },
            _ => null,
        };

    private static UpdateItemRequest? BuildUpdateRequest(string userId,
                                                         string itemClass,
                                                         string serverId,
                                                         ApplicationData? appData,
                                                         Item existing,
                                                         GhostingPolicy ghosting) =>
        itemClass switch
        {
            "Contact" when DeserializeAppData<ContactData>(appData) is { } cd =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Contact = ApplyContactChange(existing.Contact, cd, ghosting)
                },
            "Calendar" when DeserializeAppData<CalendarData>(appData) is { } cal =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Calendar = ApplyCalendarChange(existing.Calendar, cal, ghosting)
                },
            "Email" when DeserializeAppData<EmailData>(appData) is { } ed =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Email = ApplyEmailChange(existing.Email, ed)
                },
            "Task" when DeserializeAppData<TaskData>(appData) is { } td =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Task = ApplyTaskChange(existing.Task, td)
                },
            "Note" when DeserializeAppData<NoteData>(appData) is { } nd =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Note = ApplyNoteChange(existing.Note, nd)
                },
            _ => null,
        };

    private static EmailItem ApplyEmailChange(EmailItem existing, EmailData ed)
    {
        var e = existing.Clone();
        if (ed.Read.HasValue) e.Read = ed.Read != 0;

        if (ed.Flag is { } f)
        {
            // An empty <Flag/> clears the flag; a populated Flag sets it.
            bool hasAny = f.Status.HasValue || f.FlagType is not null || f.Subject is not null;
            e.Flag = hasAny ? f.ToProtoFlag() : null;
        }
        return e;
    }

    private static IReadOnlyList<BodyPreference>? SelectBodyPreference(SyncOptions? options)
    {
        var prefs = options?.BodyPreference;
        return prefs is null or { Count: 0 } ? null : prefs;
    }

    internal static ContactItem ApplyContactChange(ContactItem existing, ContactData cd) =>
        ApplyContactChange(existing, cd, GhostingPolicy.GhostAll);

    internal static ContactItem ApplyContactChange(ContactItem existing, ContactData cd, GhostingPolicy ghosting)
    {
        var merged = ApplyContactOverlay(existing, cd);
        ClearOmittedContactElements(merged, cd, ghosting);

        if (IsSelfContact(existing))
            RestoreAccountAddresses(merged, existing);

        return merged;
    }

    internal static bool IsSelfContact(ContactItem c) =>
        c.Annotation is { } a && a.HasContactType &&
        string.Equals(a.ContactType, "Me", StringComparison.OrdinalIgnoreCase);

    // the account owns these on the me contact, so a device edit never lands and the device gets the
    // server's value back on its next sync
    private static void RestoreAccountAddresses(ContactItem merged, ContactItem existing)
    {
        if (existing.HasEmail1Address) merged.Email1Address = existing.Email1Address;
        else merged.ClearEmail1Address();

        if (existing.HasImAddress) merged.ImAddress = existing.ImAddress;
        else merged.ClearImAddress();

        if (existing.HasImAddress2) merged.ImAddress2 = existing.ImAddress2;
        else merged.ClearImAddress2();
    }

    private static void ClearOmittedContactElements(ContactItem c, ContactData cd, GhostingPolicy g)
    {
        if (g.PreservesEverything) return;

        void ClearIn(string ns, string name, object? sent, Action clear)
        {
            if (sent is null && g.ShouldClear(ns, name)) clear();
        }

        void Clear(string name, object? sent, Action clear) => ClearIn(Constants.Contacts, name, sent, clear);

        // contacts2 is its own code page and its own namespace on the wire, so a Supported
        // declaration of these arrives keyed "Contacts2:" and never matches "Contacts:"
        void Clear2(string name, object? sent, Action clear) => ClearIn(Constants.Contacts2, name, sent, clear);

        Clear(nameof(cd.FirstName), cd.FirstName, c.ClearFirstName);
        Clear(nameof(cd.MiddleName), cd.MiddleName, c.ClearMiddleName);
        Clear(nameof(cd.LastName), cd.LastName, c.ClearLastName);
        Clear(nameof(cd.Title), cd.Title, c.ClearTitle);
        Clear(nameof(cd.Suffix), cd.Suffix, c.ClearSuffix);
        Clear(nameof(cd.FileAs), cd.FileAs, c.ClearFileAs);
        Clear2(nameof(cd.NickName), cd.NickName, c.ClearNickName);
        Clear(nameof(cd.YomiFirstName), cd.YomiFirstName, c.ClearYomiFirstName);
        Clear(nameof(cd.YomiLastName), cd.YomiLastName, c.ClearYomiLastName);
        Clear(nameof(cd.YomiCompanyName), cd.YomiCompanyName, c.ClearYomiCompanyName);
        Clear(nameof(cd.CompanyName), cd.CompanyName, c.ClearCompanyName);
        Clear(nameof(cd.Department), cd.Department, c.ClearDepartment);
        Clear(nameof(cd.JobTitle), cd.JobTitle, c.ClearJobTitle);
        Clear(nameof(cd.OfficeLocation), cd.OfficeLocation, c.ClearOfficeLocation);
        Clear2(nameof(cd.AccountName), cd.AccountName, c.ClearAccountName);
        Clear2(nameof(cd.ManagerName), cd.ManagerName, c.ClearManagerName);
        Clear2(nameof(cd.CustomerId), cd.CustomerId, c.ClearCustomerId);
        Clear2(nameof(cd.GovernmentId), cd.GovernmentId, c.ClearGovernmentId);
        Clear(nameof(cd.AssistantName), cd.AssistantName, c.ClearAssistantName);
        Clear(nameof(cd.Email1Address), cd.Email1Address, c.ClearEmail1Address);
        Clear(nameof(cd.Email2Address), cd.Email2Address, c.ClearEmail2Address);
        Clear(nameof(cd.Email3Address), cd.Email3Address, c.ClearEmail3Address);
        Clear(nameof(cd.BusinessPhoneNumber), cd.BusinessPhoneNumber, c.ClearBusinessPhoneNumber);
        Clear(nameof(cd.Business2PhoneNumber), cd.Business2PhoneNumber, c.ClearBusiness2PhoneNumber);
        Clear(nameof(cd.BusinessFaxNumber), cd.BusinessFaxNumber, c.ClearBusinessFaxNumber);
        Clear(nameof(cd.HomePhoneNumber), cd.HomePhoneNumber, c.ClearHomePhoneNumber);
        Clear(nameof(cd.Home2PhoneNumber), cd.Home2PhoneNumber, c.ClearHome2PhoneNumber);
        Clear(nameof(cd.HomeFaxNumber), cd.HomeFaxNumber, c.ClearHomeFaxNumber);
        Clear(nameof(cd.MobilePhoneNumber), cd.MobilePhoneNumber, c.ClearMobilePhoneNumber);
        Clear(nameof(cd.CarPhoneNumber), cd.CarPhoneNumber, c.ClearCarPhoneNumber);
        Clear(nameof(cd.PagerNumber), cd.PagerNumber, c.ClearPagerNumber);
        Clear(nameof(cd.RadioPhoneNumber), cd.RadioPhoneNumber, c.ClearRadioPhoneNumber);
        Clear(nameof(cd.AssistantPhoneNumber), cd.AssistantPhoneNumber, c.ClearAssistantPhoneNumber);
        Clear2(nameof(cd.CompanyMainPhone), cd.CompanyMainPhone, c.ClearCompanyMainPhone);
        Clear2(nameof(cd.MMS), cd.MMS, c.ClearMms);
        Clear2(nameof(cd.IMAddress), cd.IMAddress, c.ClearImAddress);
        Clear2(nameof(cd.IMAddress2), cd.IMAddress2, c.ClearImAddress2);
        Clear2(nameof(cd.IMAddress3), cd.IMAddress3, c.ClearImAddress3);
        Clear(nameof(cd.BusinessAddressStreet), cd.BusinessAddressStreet, c.ClearBusinessAddressStreet);
        Clear(nameof(cd.BusinessAddressCity), cd.BusinessAddressCity, c.ClearBusinessAddressCity);
        Clear(nameof(cd.BusinessAddressState), cd.BusinessAddressState, c.ClearBusinessAddressState);
        Clear(nameof(cd.BusinessAddressPostalCode), cd.BusinessAddressPostalCode, c.ClearBusinessAddressPostalCode);
        Clear(nameof(cd.BusinessAddressCountry), cd.BusinessAddressCountry, c.ClearBusinessAddressCountry);
        Clear(nameof(cd.HomeAddressStreet), cd.HomeAddressStreet, c.ClearHomeAddressStreet);
        Clear(nameof(cd.HomeAddressCity), cd.HomeAddressCity, c.ClearHomeAddressCity);
        Clear(nameof(cd.HomeAddressState), cd.HomeAddressState, c.ClearHomeAddressState);
        Clear(nameof(cd.HomeAddressPostalCode), cd.HomeAddressPostalCode, c.ClearHomeAddressPostalCode);
        Clear(nameof(cd.HomeAddressCountry), cd.HomeAddressCountry, c.ClearHomeAddressCountry);
        Clear(nameof(cd.OtherAddressStreet), cd.OtherAddressStreet, c.ClearOtherAddressStreet);
        Clear(nameof(cd.OtherAddressCity), cd.OtherAddressCity, c.ClearOtherAddressCity);
        Clear(nameof(cd.OtherAddressState), cd.OtherAddressState, c.ClearOtherAddressState);
        Clear(nameof(cd.OtherAddressPostalCode), cd.OtherAddressPostalCode, c.ClearOtherAddressPostalCode);
        Clear(nameof(cd.OtherAddressCountry), cd.OtherAddressCountry, c.ClearOtherAddressCountry);
        Clear(nameof(cd.Spouse), cd.Spouse, c.ClearSpouse);
        Clear(nameof(cd.WebPage), cd.WebPage, c.ClearWebPage);
        // Picture is deliberately absent: MS-ASCMD 2.2.3.24 leaves Body, Data and Picture
        // unchanged when omitted whatever Supported said. GhostingPolicy enforces this too
        Clear(nameof(cd.Birthday), cd.Birthday, () => c.Birthday = null);
        Clear(nameof(cd.Anniversary), cd.Anniversary, () => c.Anniversary = null);
        Clear(nameof(cd.Categories), cd.Categories, c.Categories.Clear);
        Clear(nameof(cd.Children), cd.Children, c.Children.Clear);
    }

    private static ContactItem ApplyContactOverlay(ContactItem existing, ContactData cd)
    {
        var c = existing.Clone();
        if (cd.FirstName is not null) c.FirstName = cd.FirstName;
        if (cd.MiddleName is not null) c.MiddleName = cd.MiddleName;
        if (cd.LastName is not null) c.LastName = cd.LastName;
        if (cd.Title is not null) c.Title = cd.Title;
        if (cd.Suffix is not null) c.Suffix = cd.Suffix;
        if (cd.FileAs is not null) c.FileAs = cd.FileAs;
        if (cd.Alias is not null) c.Alias = cd.Alias;
        if (cd.NickName is not null) c.NickName = cd.NickName;
        if (cd.YomiFirstName is not null) c.YomiFirstName = cd.YomiFirstName;
        if (cd.YomiLastName is not null) c.YomiLastName = cd.YomiLastName;
        if (cd.YomiCompanyName is not null) c.YomiCompanyName = cd.YomiCompanyName;
        if (cd.CompanyName is not null) c.CompanyName = cd.CompanyName;
        if (cd.Department is not null) c.Department = cd.Department;
        if (cd.JobTitle is not null) c.JobTitle = cd.JobTitle;
        if (cd.OfficeLocation is not null) c.OfficeLocation = cd.OfficeLocation;
        if (cd.AccountName is not null) c.AccountName = cd.AccountName;
        if (cd.ManagerName is not null) c.ManagerName = cd.ManagerName;
        if (cd.CustomerId is not null) c.CustomerId = cd.CustomerId;
        if (cd.GovernmentId is not null) c.GovernmentId = cd.GovernmentId;
        if (cd.AssistantName is not null) c.AssistantName = cd.AssistantName;
        if (cd.Email1Address is not null) c.Email1Address = cd.Email1Address;
        if (cd.Email2Address is not null) c.Email2Address = cd.Email2Address;
        if (cd.Email3Address is not null) c.Email3Address = cd.Email3Address;
        if (cd.BusinessPhoneNumber is not null) c.BusinessPhoneNumber = cd.BusinessPhoneNumber;
        if (cd.Business2PhoneNumber is not null) c.Business2PhoneNumber = cd.Business2PhoneNumber;
        if (cd.BusinessFaxNumber is not null) c.BusinessFaxNumber = cd.BusinessFaxNumber;
        if (cd.HomePhoneNumber is not null) c.HomePhoneNumber = cd.HomePhoneNumber;
        if (cd.Home2PhoneNumber is not null) c.Home2PhoneNumber = cd.Home2PhoneNumber;
        if (cd.HomeFaxNumber is not null) c.HomeFaxNumber = cd.HomeFaxNumber;
        if (cd.MobilePhoneNumber is not null) c.MobilePhoneNumber = cd.MobilePhoneNumber;
        if (cd.CarPhoneNumber is not null) c.CarPhoneNumber = cd.CarPhoneNumber;
        if (cd.PagerNumber is not null) c.PagerNumber = cd.PagerNumber;
        if (cd.RadioPhoneNumber is not null) c.RadioPhoneNumber = cd.RadioPhoneNumber;
        if (cd.AssistantPhoneNumber is not null) c.AssistantPhoneNumber = cd.AssistantPhoneNumber;
        if (cd.CompanyMainPhone is not null) c.CompanyMainPhone = cd.CompanyMainPhone;
        if (cd.MMS is not null) c.Mms = cd.MMS;
        if (cd.IMAddress is not null) c.ImAddress = cd.IMAddress;
        if (cd.IMAddress2 is not null) c.ImAddress2 = cd.IMAddress2;
        if (cd.IMAddress3 is not null) c.ImAddress3 = cd.IMAddress3;
        if (cd.BusinessAddressStreet is not null) c.BusinessAddressStreet = cd.BusinessAddressStreet;
        if (cd.BusinessAddressCity is not null) c.BusinessAddressCity = cd.BusinessAddressCity;
        if (cd.BusinessAddressState is not null) c.BusinessAddressState = cd.BusinessAddressState;
        if (cd.BusinessAddressPostalCode is not null) c.BusinessAddressPostalCode = cd.BusinessAddressPostalCode;
        if (cd.BusinessAddressCountry is not null) c.BusinessAddressCountry = cd.BusinessAddressCountry;
        if (cd.HomeAddressStreet is not null) c.HomeAddressStreet = cd.HomeAddressStreet;
        if (cd.HomeAddressCity is not null) c.HomeAddressCity = cd.HomeAddressCity;
        if (cd.HomeAddressState is not null) c.HomeAddressState = cd.HomeAddressState;
        if (cd.HomeAddressPostalCode is not null) c.HomeAddressPostalCode = cd.HomeAddressPostalCode;
        if (cd.HomeAddressCountry is not null) c.HomeAddressCountry = cd.HomeAddressCountry;
        if (cd.OtherAddressStreet is not null) c.OtherAddressStreet = cd.OtherAddressStreet;
        if (cd.OtherAddressCity is not null) c.OtherAddressCity = cd.OtherAddressCity;
        if (cd.OtherAddressState is not null) c.OtherAddressState = cd.OtherAddressState;
        if (cd.OtherAddressPostalCode is not null) c.OtherAddressPostalCode = cd.OtherAddressPostalCode;
        if (cd.OtherAddressCountry is not null) c.OtherAddressCountry = cd.OtherAddressCountry;
        if (cd.Spouse is not null) c.Spouse = cd.Spouse;
        if (cd.WebPage is not null) c.WebPage = cd.WebPage;
        if (cd.Picture is not null) c.Picture = Google.Protobuf.ByteString.CopyFrom(cd.Picture);
        if (cd.Birthday.HasValue) c.Birthday = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
            DateTime.SpecifyKind(cd.Birthday.Value, DateTimeKind.Utc));
        if (cd.Anniversary.HasValue) c.Anniversary = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
            DateTime.SpecifyKind(cd.Anniversary.Value, DateTimeKind.Utc));

        var notes = cd.Body?.Data ?? cd.BodyLegacy;
        if (notes is not null) c.Notes = notes;

        // a present container replaces wholesale; an absent one leaves the stored set alone,
        // matching how the scalar fields above treat omission
        if (cd.Categories is not null)
        {
            c.Categories.Clear();
            c.Categories.AddRange(cd.Categories.Items.Select(n => new ContactCategory
            { Id = Guid.NewGuid().ToString("N"), Name = n }));
        }

        if (cd.Children is not null)
        {
            c.Children.Clear();
            c.Children.AddRange(cd.Children.Items.Select(n => new ContactChild
            { Id = Guid.NewGuid().ToString("N"), Name = n }));
        }

        return c;
    }

    internal static CalendarItem ApplyCalendarChange(CalendarItem existing, CalendarData cd) =>
        ApplyCalendarChange(existing, cd, GhostingPolicy.GhostAll);

    internal static CalendarItem ApplyCalendarChange(CalendarItem existing, CalendarData cd, GhostingPolicy ghosting)
    {
        var merged = ApplyCalendarOverlay(existing, cd);
        ClearOmittedCalendarElements(merged, cd, ghosting);
        return merged;
    }

    // MS-ASCMD 2.2.3.179 fixes which Calendar elements may appear in Supported: fourteen are
    // required whenever the client uses it for this class, plus six optional ones. Anything outside
    // that set stays ghosted no matter what the client sent.
    private static void ClearOmittedCalendarElements(CalendarItem cal, CalendarData cd, GhostingPolicy g)
    {
        if (g.PreservesEverything) return;

        void Clear(string name, object? sent, Action clear)
        {
            if (sent is null && g.ShouldClear(Constants.Calendar, name)) clear();
        }

        // the fourteen required entries
        Clear(nameof(cd.DtStamp), cd.DtStamp, () => cal.DtStamp = null);
        Clear(nameof(cd.Categories), cd.Categories, cal.Categories.Clear);
        Clear(nameof(cd.Sensitivity), cd.Sensitivity, cal.ClearSensitivity);
        Clear(nameof(cd.BusyStatus), cd.BusyStatus, cal.ClearBusyStatus);
        Clear("UID", cd.Uid, cal.ClearUid);
        Clear(nameof(cd.Timezone), cd.Timezone, cal.ClearTimezone);
        Clear(nameof(cd.StartTime), cd.StartTime, () => cal.StartTime = null);
        Clear(nameof(cd.Subject), cd.Subject, cal.ClearSubject);
        Clear(nameof(cd.Location), cd.Location, cal.ClearLocation);
        Clear(nameof(cd.EndTime), cd.EndTime, () => cal.EndTime = null);
        Clear(nameof(cd.Recurrence), cd.Recurrence, ClearRecurrence);
        Clear(nameof(cd.AllDayEvent), cd.AllDayEvent, cal.ClearAllDayEvent);
        Clear(nameof(cd.Reminder), cd.Reminder, cal.ClearReminder);
        Clear(nameof(cd.Exceptions), cd.Exceptions, cal.Exceptions.Clear);

        // the six optional ones
        Clear(nameof(cd.Attendees), cd.Attendees, cal.Attendees.Clear);
        Clear(nameof(cd.OrganizerName), cd.OrganizerName, cal.ClearOrganizerName);
        Clear(nameof(cd.OrganizerEmail), cd.OrganizerEmail, cal.ClearOrganizerEmail);
        Clear(nameof(cd.MeetingStatus), cd.MeetingStatus, cal.ClearMeetingStatus);
        Clear(nameof(cd.ResponseRequested), cd.ResponseRequested, cal.ClearResponseRequested);
        Clear(nameof(cd.DisallowNewTimeProposal), cd.DisallowNewTimeProposal, cal.ClearDisallowNewTimeProposal);

        void ClearRecurrence()
        {
            cal.ClearRecurrenceType();
            cal.ClearRecurrenceOccurrences();
            cal.ClearRecurrenceInterval();
            cal.ClearRecurrenceWeekOfMonth();
            cal.ClearRecurrenceDayOfWeek();
            cal.ClearRecurrenceMonthOfYear();
            cal.ClearRecurrenceDayOfMonth();
            cal.ClearRecurrenceCalendarType();
            cal.ClearRecurrenceIsLeapMonth();
            cal.ClearRecurrenceFirstDayOfWeek();
            cal.RecurrenceUntil = null;
        }
    }

    private static CalendarItem ApplyCalendarOverlay(CalendarItem existing, CalendarData cd)
    {
        var cal = existing.Clone();
        if (cd.Timezone is not null) cal.Timezone = cd.Timezone;
        if (cd.StartTime.HasValue) cal.StartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(cd.StartTime.Value, DateTimeKind.Utc));
        if (cd.EndTime.HasValue) cal.EndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(cd.EndTime.Value, DateTimeKind.Utc));
        if (cd.DtStamp.HasValue) cal.DtStamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(cd.DtStamp.Value, DateTimeKind.Utc));
        if (cd.Uid is not null) cal.Uid = cd.Uid;
        if (cd.Subject is not null) cal.Subject = cd.Subject;
        if (cd.Location is not null) cal.Location = cd.Location;
        if (cd.OrganizerName is not null) cal.OrganizerName = cd.OrganizerName;
        if (cd.OrganizerEmail is not null) cal.OrganizerEmail = cd.OrganizerEmail;
        if (cd.Reminder.HasValue) cal.Reminder = cd.Reminder.Value;
        if (cd.AllDayEvent.HasValue) cal.AllDayEvent = cd.AllDayEvent != 0;
        if (cd.BusyStatus.HasValue) cal.BusyStatus = cd.BusyStatus.Value;
        if (cd.Sensitivity.HasValue) cal.Sensitivity = cd.Sensitivity.Value;
        if (cd.MeetingStatus.HasValue) cal.MeetingStatus = cd.MeetingStatus.Value;
        // ResponseType / AppointmentReplyTime / OnlineMeeting* are server-owned; a client echo is ignored
        if (cd.ResponseRequested.HasValue) cal.ResponseRequested = cd.ResponseRequested != 0;
        if (cd.DisallowNewTimeProposal.HasValue) cal.DisallowNewTimeProposal = cd.DisallowNewTimeProposal != 0;
        var notes = cd.Body?.Data ?? cd.BodyLegacy;
        if (notes is not null) cal.Notes = notes;
        if (cd.BodyTruncated.HasValue) cal.BodyTruncated = cd.BodyTruncated != 0;
        if (cd.Recurrence is { } r)
        {
            if (r.Type.HasValue) cal.RecurrenceType = r.Type.Value;
            if (r.Occurrences.HasValue) cal.RecurrenceOccurrences = r.Occurrences.Value;
            if (r.Interval.HasValue) cal.RecurrenceInterval = r.Interval.Value;
            if (r.WeekOfMonth.HasValue) cal.RecurrenceWeekOfMonth = r.WeekOfMonth.Value;
            if (r.DayOfWeek.HasValue) cal.RecurrenceDayOfWeek = r.DayOfWeek.Value;
            if (r.MonthOfYear.HasValue) cal.RecurrenceMonthOfYear = r.MonthOfYear.Value;
            if (r.DayOfMonth.HasValue) cal.RecurrenceDayOfMonth = r.DayOfMonth.Value;
            if (r.CalendarType.HasValue) cal.RecurrenceCalendarType = r.CalendarType.Value;
            if (r.IsLeapMonth.HasValue) cal.RecurrenceIsLeapMonth = r.IsLeapMonth != 0;
            if (r.FirstDayOfWeek.HasValue) cal.RecurrenceFirstDayOfWeek = r.FirstDayOfWeek.Value;
        }

        // as with contacts: present container replaces, absent container preserves
        if (cd.Attendees is not null)
        {
            cal.Attendees.Clear();
            cal.Attendees.AddRange(ProtoExtensions.ToProtoAttendees(cd.Attendees));
        }

        if (cd.Categories is not null)
        {
            cal.Categories.Clear();
            cal.Categories.AddRange(ProtoExtensions.ToProtoCategories(cd.Categories));
        }

        if (cd.Exceptions is not null)
        {
            cal.Exceptions.Clear();
            cal.Exceptions.AddRange(ProtoExtensions.ToProtoExceptions(cd.Exceptions));
        }

        return cal;
    }

    private static TaskItem ApplyTaskChange(TaskItem existing, TaskData td)
    {
        var t = existing.Clone();
        if (td.Subject is not null) t.Subject = td.Subject;
        if (td.Importance.HasValue) t.Importance = td.Importance.Value;
        if (td.Sensitivity.HasValue) t.Sensitivity = td.Sensitivity.Value;
        if (td.StartDate.HasValue) t.StartDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.StartDate.Value, DateTimeKind.Utc));
        if (td.UtcStartDate.HasValue) t.UtcStartDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.UtcStartDate.Value, DateTimeKind.Utc));
        if (td.DueDate.HasValue) t.DueDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.DueDate.Value, DateTimeKind.Utc));
        if (td.UtcDueDate.HasValue) t.UtcDueDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.UtcDueDate.Value, DateTimeKind.Utc));
        if (td.Complete.HasValue) t.Complete = td.Complete != 0;
        if (td.DateCompleted.HasValue) t.DateCompleted = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.DateCompleted.Value, DateTimeKind.Utc));
        if (td.ReminderSet.HasValue) t.ReminderSet = td.ReminderSet != 0;
        if (td.ReminderTime.HasValue) t.ReminderTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(td.ReminderTime.Value, DateTimeKind.Utc));
        var notes = td.Body?.Data;
        if (notes is not null) t.Notes = notes;
        if (td.NativeBodyType.HasValue) t.NativeBodyType = td.NativeBodyType.Value;
        if (td.Recurrence is { } r)
        {
            if (r.Type.HasValue) t.RecurrenceType = r.Type.Value;
            if (r.Start.HasValue) t.RecurrenceStart = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(r.Start.Value, DateTimeKind.Utc));
            if (r.Until.HasValue) t.RecurrenceUntil = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(r.Until.Value, DateTimeKind.Utc));
            if (r.Occurrences.HasValue) t.RecurrenceOccurrences = r.Occurrences.Value;
            if (r.Interval.HasValue) t.RecurrenceInterval = r.Interval.Value;
            if (r.DayOfWeek.HasValue) t.RecurrenceDayOfWeek = r.DayOfWeek.Value;
            if (r.DayOfMonth.HasValue) t.RecurrenceDayOfMonth = r.DayOfMonth.Value;
            if (r.WeekOfMonth.HasValue) t.RecurrenceWeekOfMonth = r.WeekOfMonth.Value;
            if (r.MonthOfYear.HasValue) t.RecurrenceMonthOfYear = r.MonthOfYear.Value;
            if (r.Regenerate.HasValue) t.RecurrenceRegenerate = r.Regenerate != 0;
            if (r.DeadOccur.HasValue) t.RecurrenceDeadOccur = r.DeadOccur.Value;
            if (r.CalendarType.HasValue) t.RecurrenceCalendarType = r.CalendarType.Value;
            if (r.IsLeapMonth.HasValue) t.RecurrenceIsLeapMonth = r.IsLeapMonth != 0;
            if (r.FirstDayOfWeek.HasValue) t.RecurrenceFirstDayOfWeek = r.FirstDayOfWeek.Value;
        }
        return t;
    }

    private static NoteItem ApplyNoteChange(NoteItem existing, NoteData nd)
    {
        var n = existing.Clone();
        // absent Subject/Body is not an implicit delete
        if (nd.Subject is not null) n.Subject = nd.Subject;
        var body = nd.Body?.Data;
        if (body is not null) n.Body = body;
        if (nd.NativeBodyType.HasValue) n.NativeBodyType = nd.NativeBodyType.Value;
        if (nd.MessageClass is not null) n.MessageClass = nd.MessageClass;
        n.LastModifiedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
        // a missing Categories element means delete: replace wholesale with what the client sent
        n.Categories.Clear();
        n.Categories.AddRange(nd.Categories?.Items.Select(c => new NoteCategory
        { Id = Guid.NewGuid().ToString("N"), Category = c }) ?? []);
        return n;
    }

    // Serialize, but never let a single malformed stored item throw out of the sync batch.
    private ApplicationData? TrySerialize(Item item,
                                          IReadOnlySet<string>? requestedAnnotations = null,
                                          IReadOnlyList<BodyPreference>? bodyPrefs = null)
    {
        try
        {
            return Serialize(item, requestedAnnotations, bodyPrefs);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "dropping unserializable item {ServerId} from sync", item.ServerId);
            return null;
        }
    }

    private static ApplicationData? Serialize(Item item,
                                              IReadOnlySet<string>? requestedAnnotations = null,
                                              IReadOnlyList<BodyPreference>? bodyPrefs = null)
    {
        var appData = item.BodyCase switch
        {
            Item.BodyOneofCase.Contact => item.Contact.ToApplicationData(),
            Item.BodyOneofCase.Calendar => item.Calendar.ToApplicationData(),
            Item.BodyOneofCase.Task => item.Task.ToApplicationData(),
            Item.BodyOneofCase.Email => item.Email.ToApplicationData(bodyPrefs),
            Item.BodyOneofCase.Note => item.Note.ToApplicationData(),
            _ => null,
        };

        if (appData is not null && item.BodyCase == Item.BodyOneofCase.Contact
            && item.Contact.Annotation is not null && requestedAnnotations is { Count: > 0 })
        {
            var annotations = BuildAnnotations(item.Contact.Annotation, requestedAnnotations);
            if (annotations is not null)
                InjectAnnotations(appData, annotations);
        }

        return appData;
    }

    private static Annotations? BuildAnnotations(ContactAnnotation ann, IReadOnlySet<string> requested)
    {
        var items = new List<Annotation>();

        void Add(string name, string? value)
        {
            if (requested.Contains(name) && value is not null)
                items.Add(new Annotation { Name = name, Value = value });
        }

        Add("WLID", ann.HasWlId ? ann.WlId : null);
        Add("IMMRI", ann.HasImMri ? ann.ImMri : null);
        Add("Type", ann.HasContactType ? ann.ContactType : null);
        Add("UserTileUrl", ann.HasUserTileUrl ? ann.UserTileUrl : null);
        Add("UserTileHash", ann.HasUserTileHash ? ann.UserTileHash : null);
        Add("TrustLevel", ann.HasTrustLevel ? ann.TrustLevel.ToString() : null);
        Add("FavoriteOrder", ann.HasFavoriteOrder ? ann.FavoriteOrder.ToString() : null);

        bool isSelf = ann.HasContactType &&
              string.Equals(ann.ContactType, "Me", StringComparison.OrdinalIgnoreCase);

        // OID/CID on the current user's own contact makes WP7 reject the entire contact sync
        if (!isSelf)
        {
            Add("OID", ann.HasObjectId ? ann.ObjectId : null);
            Add("CID", ann.HasCid ? ann.Cid.ToString("x16") : null);
        }

        return items.Count > 0 ? new Annotations { Items = items } : null;
    }

    private static void InjectAnnotations(ApplicationData appData, Annotations annotations)
    {
        var serializer = new XmlSerializer(typeof(Annotations));
        var doc = new XmlDocument();
        using var sw = new StringWriter();
        serializer.Serialize(sw, annotations);
        doc.LoadXml(sw.ToString());
        appData.Elements.Add(doc.DocumentElement!);
    }

    private async IAsyncEnumerable<Item> ReadItemsByIdsAsync(string userId, List<string> serverIds,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = mailbox.GetItems(new GetItemsRequest { UserId = userId, ServerIds = { serverIds } }, cancellationToken: ct);
        await foreach (var i in call.ResponseStream.ReadAllAsync(ct))
            yield return i;
    }

    private async IAsyncEnumerable<ItemEvent> ReadItemEventsAsync(
        string userId, string collectionId, long afterWatermark, [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = mailbox.GetItemEvents(
            new GetItemEventsRequest { UserId = userId, CollectionId = collectionId, AfterWatermark = afterWatermark }, cancellationToken: ct);
        await foreach (var e in call.ResponseStream.ReadAllAsync(ct))
            yield return e;
    }


    private async Task<string> GetItemClassAsync(string userId, string collectionId, CancellationToken ct)
    {
        try
        {
            var folder = await mailbox.GetFolderAsync(
                new GetFolderRequest { UserId = userId, ServerId = collectionId },
                cancellationToken: ct);
            return folder.Type switch
            {
                ProtoFolderType.CalendarDefault or ProtoFolderType.Calendar => "Calendar",
                ProtoFolderType.ContactsDefault or ProtoFolderType.Contacts or ProtoFolderType.MeContact => "Contact",
                ProtoFolderType.TasksDefault or ProtoFolderType.Task => "Task",
                ProtoFolderType.NotesDefault or ProtoFolderType.Notes => "Note",
                _ => "Email",
            };
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return "Email";
        }
    }

    private static IReadOnlySet<string>? ParseCachedAnnotationNames(string? cached)
    {
        if (string.IsNullOrEmpty(cached)) return null;

        return cached.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
    }

    private static T? DeserializeAppData<T>(ApplicationData? appData) where T : class
    {
        if (appData?.Elements is null or { Count: 0 }) return null;
        var doc = new XmlDocument();
        var root = doc.CreateElement("ApplicationData", Constants.AirSync);
        doc.AppendChild(root);
        foreach (var el in appData.Elements)
            root.AppendChild(doc.ImportNode(el, true));
        var serializer = new XmlSerializer(typeof(T));
        return (T?)serializer.Deserialize(new XmlNodeReader(doc));
    }
}
