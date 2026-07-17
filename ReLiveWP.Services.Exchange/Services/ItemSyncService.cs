using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;
using ReLiveWP.Services.Exchange.Extensions;

namespace ReLiveWP.Services.Exchange.Services;

public class ItemSyncService(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<ItemSyncService> logger)
{
    private static readonly IReadOnlySet<string> EmptyServerIds = new HashSet<string>();

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

    public async Task<List<string>> ListDeviceCollectionIdsAsync(string userId, string deviceId, CancellationToken ct = default)
    {
        var ids = new List<string>();
        using var call = mailbox.ListSyncStates(
            new ListSyncStatesRequest { UserId = userId, DeviceId = deviceId }, cancellationToken: ct);
        await foreach (var s in call.ResponseStream.ReadAllAsync(ct))
            ids.Add(s.CollectionId);
        return ids;
    }

    public async Task<SyncCollection?> SyncByCollectionIdAsync(string userId,
                                                               string deviceId,
                                                               string collectionId,
                                                               CancellationToken ct = default)
    {
        SyncState state;
        try
        {
            state = await mailbox.GetSyncStateAsync(
                new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = collectionId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return null;
        }

        var request = new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, GetChanges = true };
        return await SyncAsync(userId, deviceId, request, ct);
    }

    public async Task<SyncCollection> SyncAsync(string userId,
                                                string deviceId,
                                                SyncCollection request,
                                                CancellationToken ct = default)
    {
        var collectionId = request.CollectionId;
        bool hasClientCommands = request.Commands is { } reqCmds &&
            (reqCmds.Add.Count > 0 || reqCmds.Change.Count > 0 || reqCmds.Delete.Count > 0 || reqCmds.Fetch.Count > 0);
        // explicit GetChanges wins; if absent it defaults to false only at key 0
        bool getChanges = request.GetChanges ?? (request.SyncKey != "0");

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
        int windowSize = SyncEngine.ResolveWindowSize(request.WindowSize);
        // MS-ASCMD 2.2.3.116: MoreAvailable is reported only if the client's request actually
        // included a WindowSize element (a missing element still gets the 100-item default window,
        // it just never advertises more content beyond it)
        bool windowSizeSent = request.WindowSize.HasValue;

        if (request.SyncKey == "0")
        {
            var annotationNames = request.Options?.Annotations?.RequestedNames();
            result = await InitialSyncAsync(userId, deviceId, collectionId, state, annotationNames, ct);
            serverChangedIds = EmptyServerIds;
        }
        else if (state is not null && request.SyncKey == state.PreviousSyncKey && request.SyncKey != state.SyncKey)
        {
            // client resent a key it never got a response for: roll back to that checkpoint and
            // recompute from current code rather than replay a stored blob, so retries and
            // command resends are handled the same way
            state = new SyncState
            {
                UserId = userId,
                DeviceId = deviceId,
                CollectionId = collectionId,
                SyncKey = request.SyncKey,
                Watermark = state.PreviousWatermark,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
                PreviousSyncKey = state.PreviousSyncKey,
                PreviousWatermark = state.PreviousWatermark,
            };

            var annotationNames = ParseCachedAnnotationNames(state.CachedAnnotationNames);
            var bodyPref = SelectBodyPreference(request.Options);
            (result, serverChangedIds) = await IncrementalSyncAsync(userId, collectionId, state, getChanges, hasClientCommands, annotationNames, bodyPref, windowSize, windowSizeSent, ct);
        }
        else if (state is null || state.SyncKey != request.SyncKey)
        {
            // key is neither current nor the one-deep checkpoint: genuinely out of window, re-prime from 0
            if (state is not null)
                await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    CollectionId = collectionId,
                    SyncKey = "0",
                    Watermark = 0,
                    CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
                    PreviousSyncKey = "0",
                    PreviousWatermark = 0,
                }, cancellationToken: ct);

            return new SyncCollection { CollectionId = collectionId, SyncKey = "0", Status = 3 };
        }
        else
        {
            var annotationNames = ParseCachedAnnotationNames(state.CachedAnnotationNames);
            var bodyPref = SelectBodyPreference(request.Options);
            (result, serverChangedIds) = await IncrementalSyncAsync(userId, collectionId, state, getChanges, hasClientCommands, annotationNames, bodyPref, windowSize, windowSizeSent, ct);
        }

        if (request.Commands is { } cmds &&
            (cmds.Add.Count > 0 || cmds.Change.Count > 0 || cmds.Delete.Count > 0 || cmds.Fetch.Count > 0))
        {
            var itemClass = await GetItemClassAsync(userId, collectionId, ct);
            var bodyPref = SelectBodyPreference(request.Options);
            var conflictPolicy = request.Options?.Conflict ?? SyncConflict.ServerWins;
            var responses = await ProcessClientCommandsAsync(userId, collectionId, itemClass, cmds, bodyPref, serverChangedIds, conflictPolicy, ct);
            if (responses is not null)
                result.Responses = responses;

            // never hand back a server Add/Change for an item the client just deleted: re-adding what
            // it asked to remove wedges WP7 into resending the Delete forever
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
        BodyPreference? bodyPref,
        int windowSize,
        bool windowSizeSent,
        CancellationToken ct)
    {
        // records a one-deep checkpoint on advance so a retransmit of the prior key can roll back
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
                PreviousSyncKey = advanced ? state.SyncKey : state.PreviousSyncKey,
                PreviousWatermark = advanced ? state.Watermark : state.PreviousWatermark,
            }, cancellationToken: ct).ResponseAsync;
        }

        // -1 means "never synced": treat as watermark 0 so the very first real sync flows through
        // the same ItemEvent-log-driven path (and windowing) as every incremental sync
        long baseline = state.Watermark == -1 ? 0 : state.Watermark;

        // commands must still advance the key or the client treats the batch as uncommitted and
        // resends it. GetChanges=0 means don't surface content, but a client Change in the same
        // request still needs the server-changed-id set for conflict detection, so events are
        // still read here (unwindowed: nothing is being surfaced, so there's nothing to page),
        // just without advancing the stored watermark past content the client was never shown.
        if (!getChanges)
        {
            var noContentEvents = new List<SyncEvent>();
            await foreach (var e in ReadItemEventsAsync(userId, collectionId, baseline, ct))
                noContentEvents.Add(new SyncEvent(e.Id, e.ServerId, e.EventType));
            var noContentDelta = SyncEngine.Collapse(noContentEvents);

            var key = hasClientCommands ? SyncEngine.NextSyncKey(state.SyncKey) : state.SyncKey;
            await UpsertAdvancedAsync(key, state.Watermark);
            return (new SyncCollection { CollectionId = collectionId, SyncKey = key, Status = 1 }, noContentDelta.AllUpdatedServerIds);
        }

        var events = new List<SyncEvent>();
        await foreach (var e in ReadItemEventsAsync(userId, collectionId, baseline, ct))
            events.Add(new SyncEvent(e.Id, e.ServerId, e.EventType));

        if (events.Count == 0)
        {
            var key = hasClientCommands ? SyncEngine.NextSyncKey(state.SyncKey) : state.SyncKey;
            await UpsertAdvancedAsync(key, state.Watermark);
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
            var data = TrySerialize(item, requestedAnnotations, bodyPref);
            if (data is not null) cmds.Add.Add(new SyncAdd { ServerId = id, ApplicationData = data });
        }
        foreach (var id in delta.Updated)
        {
            if (!itemMap.TryGetValue(id, out var item)) continue;
            var data = TrySerialize(item, requestedAnnotations, bodyPref);
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
                                                                  BodyPreference? bodyPref,
                                                                  IReadOnlySet<string> serverChangedIds,
                                                                  SyncConflict conflictPolicy,
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
            int status = await HandleChangeAsync(userId, itemClass, change, serverChangedIds, conflictPolicy, ct);
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
            responses.Fetch.Add(await HandleFetchAsync(userId, fetch.ServerId, bodyPref, ct));

        bool any = responses.Add.Count + responses.Change.Count + responses.Delete.Count + responses.Fetch.Count > 0;
        return any ? responses : null;
    }

    // some devices fetch message bodies with an embedded fetch
    private async Task<SyncResponseFetch> HandleFetchAsync(string userId,
                                                           string serverId,
                                                           BodyPreference? bodyPref,
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

        var appData = TrySerialize(item, requestedAnnotations: null, bodyPref);
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

    private async Task<int> HandleChangeAsync(string userId,
                                              string itemClass,
                                              SyncChange change,
                                              IReadOnlySet<string> serverChangedIds,
                                              SyncConflict conflictPolicy,
                                              CancellationToken ct)
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

        // MS-ASCMD 2.2.3.34 Conflict: server-wins (default/Conflict=1) discards the client's
        // change and reports Status 7 when the item also changed server-side since the
        // client's last sync; Conflict=0 is client-wins and always applies the change below
        if (conflictPolicy == SyncConflict.ServerWins && serverChangedIds.Contains(change.ServerId))
        {
            logger.LogInformation(
                "Sync Change conflict for {ServerId}: server-wins, discarding client change", change.ServerId);
            return 7;
        }

        UpdateItemRequest? req;
        try
        {
            req = BuildUpdateRequest(userId, itemClass, change.ServerId, change.ApplicationData, existing);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Sync Change: unparseable {ItemClass} content for {ServerId}", itemClass, change.ServerId);
            return 6;
        }

        if (req is null) return 8;

        try
        {
            var result = await mailbox.UpdateItemAsync(req, cancellationToken: ct);
            return result.Found ? 1 : 8;
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
        {
            logger.LogWarning("Sync Change rejected for {ServerId}: {Detail}", change.ServerId, e.Status.Detail);
            return 6;
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
                                                         Item existing) =>
        itemClass switch
        {
            "Contact" when DeserializeAppData<ContactData>(appData) is { } cd =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Contact = ApplyContactChange(existing.Contact, cd)
                },
            "Calendar" when DeserializeAppData<CalendarData>(appData) is { } cal =>
                new UpdateItemRequest
                {
                    UserId = userId,
                    ServerId = serverId,
                    Calendar = ApplyCalendarChange(existing.Calendar, cal)
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

    private static BodyPreference? SelectBodyPreference(SyncOptions? options)
    {
        var prefs = options?.BodyPreference;
        if (prefs is null or { Count: 0 }) return null;
        return prefs.FirstOrDefault(p => p.Type == BodyType.HTML) ?? prefs[0];
    }

    private static ContactItem ApplyContactChange(ContactItem existing, ContactData cd)
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

        return c;
    }

    private static CalendarItem ApplyCalendarChange(CalendarItem existing, CalendarData cd)
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
                                          BodyPreference? bodyPref = null)
    {
        try
        {
            return Serialize(item, requestedAnnotations, bodyPref);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "dropping unserializable item {ServerId} from sync", item.ServerId);
            return null;
        }
    }

    private static ApplicationData? Serialize(Item item,
                                              IReadOnlySet<string>? requestedAnnotations = null,
                                              BodyPreference? bodyPref = null)
    {
        var appData = item.BodyCase switch
        {
            Item.BodyOneofCase.Contact => item.Contact.ToApplicationData(),
            Item.BodyOneofCase.Calendar => item.Calendar.ToApplicationData(),
            Item.BodyOneofCase.Task => item.Task.ToApplicationData(),
            Item.BodyOneofCase.Email => item.Email.ToApplicationData(bodyPref),
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
