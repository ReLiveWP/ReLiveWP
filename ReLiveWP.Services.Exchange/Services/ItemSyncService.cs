using System.Xml;
using System.Xml.Serialization;
using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;
using ReLiveWP.Services.Exchange.Extensions;

namespace ReLiveWP.Services.Exchange.Services;

// Backs per-collection item sync (email, contacts, calendar, tasks).
// Data access is via the MailboxStore gRPC client. All sync logic
// (SyncKey lifecycle, SyncEngine.Collapse, XML serialization) stays here.
public class ItemSyncService(MailboxStore.MailboxStoreClient mailbox)
{
    public async Task<SyncCollection> SyncAsync(
        string userId, string deviceId, SyncCollection request, CancellationToken ct = default)
    {
        var collectionId = request.CollectionId;
        bool getChanges = request.GetChanges.GetValueOrDefault(1) != 0;

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

        if (request.SyncKey == "0")
        {
            var annotationNames = request.Options?.Annotations?.RequestedNames();
            result = await InitialSyncAsync(userId, deviceId, collectionId, state, annotationNames, ct);
        }
        else if (state is null || state.SyncKey != request.SyncKey)
        {
            if (state is not null)
                await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    CollectionId = collectionId,
                    SyncKey = "0",
                    Watermark = 0,
                }, cancellationToken: ct);

            return new SyncCollection { CollectionId = collectionId, SyncKey = "0", Status = 3 };
        }
        else
        {
            var annotationNames = ParseCachedAnnotationNames(state.CachedAnnotationNames);
            result = await IncrementalSyncAsync(userId, collectionId, state, getChanges, annotationNames, ct);
        }

        if (request.Commands is { } cmds && (cmds.Add.Count > 0 || cmds.Change.Count > 0 || cmds.Delete.Count > 0))
        {
            var itemClass = await GetItemClassAsync(userId, collectionId, ct);
            var responses = await ProcessClientCommandsAsync(userId, collectionId, itemClass, cmds, ct);
            if (responses is not null)
                result.Responses = responses;
        }

        return result;
    }

    // ── SyncKey=0 initialisation ──────────────────────────────────────────────

    private async Task<SyncCollection> InitialSyncAsync(
        string userId, string deviceId, string collectionId, SyncState? state,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        var cached = requestedAnnotations is { Count: > 0 }
            ? string.Join(",", requestedAnnotations) : null;

        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = deviceId,
            CollectionId = collectionId,
            SyncKey = "1",
            Watermark = -1,
            CachedAnnotationNames = cached ?? string.Empty,
        }, cancellationToken: ct);

        return new SyncCollection { CollectionId = collectionId, SyncKey = "1", Status = 1 };
    }

    // ── Incremental / first-real-sync ─────────────────────────────────────────

    private async Task<SyncCollection> IncrementalSyncAsync(
        string userId, string collectionId, SyncState state, bool getChanges,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        if (state.Watermark == -1)
        {
            // First real sync: enumerate all live items + advance watermark.
            var tip = (await mailbox.GetItemEventTipAsync(
                new ItemEventTipRequest { UserId = userId, CollectionId = collectionId },
                cancellationToken: ct)).Value;

            var items = await ReadAllItemsAsync(userId, collectionId, ct);

            var newKey = SyncEngine.NextSyncKey(state.SyncKey);
            await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = state.DeviceId,
                CollectionId = collectionId,
                SyncKey = newKey,
                Watermark = tip,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
            }, cancellationToken: ct);

            var commands = items.Count > 0 ? new SyncCommands
            {
                Add = [.. items.Select(i => new SyncAdd { ServerId = i.ServerId, ApplicationData = Serialize(i, requestedAnnotations) })],
            } : null;

            return new SyncCollection { CollectionId = collectionId, SyncKey = newKey, Status = 1, Commands = commands };
        }

        if (!getChanges)
        {
            await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = state.DeviceId,
                CollectionId = collectionId,
                SyncKey = state.SyncKey,
                Watermark = state.Watermark,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
            }, cancellationToken: ct);
            return new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, Status = 1 };
        }

        var events = await ReadAllItemEventsAsync(userId, collectionId, state.Watermark, ct);

        if (events.Count == 0)
        {
            await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
            {
                UserId = userId,
                DeviceId = state.DeviceId,
                CollectionId = collectionId,
                SyncKey = state.SyncKey,
                Watermark = state.Watermark,
                CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
            }, cancellationToken: ct);

            return new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, Status = 1 };
        }

        var delta = SyncEngine.Collapse(events.Select(e => new SyncEvent(e.Id, e.ServerId, e.EventType)).ToList());
        var ids = delta.Added.Concat(delta.Updated).ToList();
        var itemMap = (await ReadItemsByIdsAsync(userId, ids, ct)).ToDictionary(i => i.ServerId);

        var cmds = new SyncCommands
        {
            Add = [.. delta.Added.Where(itemMap.ContainsKey).Select(id => new SyncAdd { ServerId = id, ApplicationData = Serialize(itemMap[id], requestedAnnotations) })],
            Change = [.. delta.Updated.Where(itemMap.ContainsKey).Select(id => new SyncChange { ServerId = id, ApplicationData = Serialize(itemMap[id], requestedAnnotations) })],
            Delete = [.. delta.Deleted.Select(id => new SyncItemRef { ServerId = id })],
        };

        var nextKey = SyncEngine.NextSyncKey(state.SyncKey);
        await mailbox.UpsertSyncStateAsync(new UpsertSyncStateRequest
        {
            UserId = userId,
            DeviceId = state.DeviceId,
            CollectionId = collectionId,
            SyncKey = nextKey,
            Watermark = delta.Watermark,
            CachedAnnotationNames = state.CachedAnnotationNames ?? string.Empty,
        }, cancellationToken: ct);

        bool hasChanges = cmds.Add.Count + cmds.Change.Count + cmds.Delete.Count > 0;
        return new SyncCollection
        {
            CollectionId = collectionId,
            SyncKey = nextKey,
            Status = 1,
            Commands = hasChanges ? cmds : null,
        };
    }

    // ── Client command handling ───────────────────────────────────────────────

    private async Task<SyncResponses?> ProcessClientCommandsAsync(
        string userId, string collectionId, string itemClass, SyncCommands cmds, CancellationToken ct)
    {
        var responses = new SyncResponses();

        foreach (var add in cmds.Add)
        {
            var (serverId, status) = await HandleAddAsync(userId, collectionId, itemClass, add.ApplicationData, ct);
            responses.Add.Add(new SyncResponseAdd
            {
                ClientId = add.ClientId ?? string.Empty,
                ServerId = serverId,
                Status = status,
            });
        }

        foreach (var change in cmds.Change)
        {
            int status = await HandleChangeAsync(userId, itemClass, change, ct);
            if (status != 1)
                responses.Change.Add(new SyncResponseChange { ServerId = change.ServerId, Status = status });
        }

        foreach (var delete in cmds.Delete)
        {
            var result = await mailbox.DeleteItemAsync(
                new DeleteItemRequest { UserId = userId, ServerId = delete.ServerId },
                cancellationToken: ct);
            if (!result.Found)
                responses.Change.Add(new SyncResponseChange { ServerId = delete.ServerId, Status = 8 });
        }

        bool any = responses.Add.Count + responses.Change.Count + responses.Fetch.Count > 0;
        return any ? responses : null;
    }

    private async Task<(string? serverId, int status)> HandleAddAsync(
        string userId, string collectionId, string itemClass, ApplicationData? appData, CancellationToken ct)
    {
        var req = BuildCreateRequest(userId, collectionId, itemClass, appData);
        if (req is null) return (null, 6);
        var item = await mailbox.CreateItemAsync(req, cancellationToken: ct);
        return (item.ServerId, 1);
    }

    private async Task<int> HandleChangeAsync(
        string userId, string itemClass, SyncChange change, CancellationToken ct)
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

        var req = BuildUpdateRequest(userId, itemClass, change.ServerId, change.ApplicationData, existing);
        if (req is null) return 8;

        var result = await mailbox.UpdateItemAsync(req, cancellationToken: ct);
        return result.Found ? 1 : 8;
    }

    // ── Request builders ──────────────────────────────────────────────────────

    private static CreateItemRequest? BuildCreateRequest(
        string userId, string collectionId, string itemClass, ApplicationData? appData) =>
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
            _ => null,
        };

    private static UpdateItemRequest? BuildUpdateRequest(
        string userId, string itemClass, string serverId, ApplicationData? appData, Item existing) =>
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
            _ => null,
        };

    // ── EAS XML ↔ proto item field mapping ────────────────────────────────────

    // These map EAS XML DTOs (from WBXML decode) → proto ContactItem / CalendarItem.
    // The proto then goes to MailboxStore for persistence. On the read side, the stored
    // proto is serialized back to EAS XML for the device response.
    private static ContactItem ApplyContactChange(ContactItem existing, ContactData cd)
    {
        // Full replace: start from the stored state and overwrite with EAS payload.
        // This preserves server-side fields (WeightedRank, annotation) not present in
        // the client's Change payload.
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
        if (cd.ResponseType.HasValue) cal.ResponseType = cd.ResponseType.Value;
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

    // ── Serialization: proto Item → EAS ApplicationData ──────────────────────

    private static ApplicationData? Serialize(Item item, IReadOnlySet<string>? requestedAnnotations = null)
    {
        var appData = item.BodyCase switch
        {
            Item.BodyOneofCase.Contact => item.Contact.ToApplicationData(),
            Item.BodyOneofCase.Calendar => item.Calendar.ToApplicationData(),
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

        Add("CID", ann.HasCid ? ann.Cid.ToString() : null);
        Add("OID", ann.HasObjectId ? ann.ObjectId : null);
        Add("WLID", ann.HasWlId ? ann.WlId : null);
        Add("IMMRI", ann.HasImMri ? ann.ImMri : null);
        Add("Type", ann.HasContactType ? ann.ContactType : null);
        Add("UserTileUrl", ann.HasUserTileUrl ? ann.UserTileUrl : null);
        Add("UserTileHash", ann.HasUserTileHash ? ann.UserTileHash : null);
        Add("TrustLevel", ann.HasTrustLevel ? ann.TrustLevel.ToString() : null);
        Add("FavoriteOrder", ann.HasFavoriteOrder ? ann.FavoriteOrder.ToString() : null);

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

    // ── gRPC stream helpers ───────────────────────────────────────────────────

    private async Task<List<Item>> ReadAllItemsAsync(string userId, string collectionId, CancellationToken ct)
    {
        var result = new List<Item>();
        using var call = mailbox.ListItems(new ListItemsRequest
        { UserId = userId, CollectionId = collectionId, IncludeDeleted = false }, cancellationToken: ct);
        await foreach (var i in call.ResponseStream.ReadAllAsync(ct))
            result.Add(i);
        return result;
    }

    private async Task<List<Item>> ReadItemsByIdsAsync(string userId, List<string> serverIds, CancellationToken ct)
    {
        var result = new List<Item>();
        using var call = mailbox.GetItems(new GetItemsRequest { UserId = userId, ServerIds = { serverIds } }, cancellationToken: ct);
        await foreach (var i in call.ResponseStream.ReadAllAsync(ct))
            result.Add(i);
        return result;
    }

    private async Task<List<ItemEvent>> ReadAllItemEventsAsync(
        string userId, string collectionId, long afterWatermark, CancellationToken ct)
    {
        var result = new List<ItemEvent>();
        using var call = mailbox.GetItemEvents(new GetItemEventsRequest
        { UserId = userId, CollectionId = collectionId, AfterWatermark = afterWatermark }, cancellationToken: ct);
        await foreach (var e in call.ResponseStream.ReadAllAsync(ct))
            result.Add(e);
        return result;
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
