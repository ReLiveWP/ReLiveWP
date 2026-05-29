using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

// Backs per-collection item sync (email, contacts, calendar, tasks).
// Uses the same event-sourcing watermark pattern as FolderSyncService via SyncEngine.Collapse.
//
// SyncKey lifecycle:
//   "0"  → client is initialising; server allocates state and returns SyncKey="1", no items.
//   "1"  → first real sync; server returns all current items as Adds (watermark sentinel = -1).
//   "N"  → incremental; server returns only changes since the previous watermark.
//
// Recovery sync (client re-sends the previous SyncKey after a dropped response) is not yet
// supported; an InvalidSyncKey (status 3) is returned instead.
public class ItemSyncService
{
    private readonly ExchangeDbContext _db;
    private readonly UserService _users;

    public ItemSyncService(ExchangeDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<SyncCollection> SyncAsync(
        string userId, string deviceId, SyncCollection request, CancellationToken ct = default)
    {
        var collectionId = request.CollectionId;
        // GetChanges defaults to 1 (true) when absent per spec.
        bool getChanges = request.GetChanges.GetValueOrDefault(1) != 0;

        // Extract annotation subscription for this collection (may be null = no annotations)
        var requestedAnnotations = request.Options?.Annotations?.RequestedNames();

        var state = await _db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == userId && s.DeviceId == deviceId && s.CollectionId == collectionId, ct);

        SyncCollection result;

        if (request.SyncKey == "0")
        {
            result = await InitialSyncAsync(userId, deviceId, collectionId, state, ct);
        }
        else if (state is null || state.SyncKey != request.SyncKey)
        {
            if (state is not null)
            {
                state.SyncKey = "0";
                state.Watermark = 0;
                state.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return new SyncCollection { CollectionId = collectionId, SyncKey = "0", Status = 3 };
        }
        else
        {
            result = await IncrementalSyncAsync(userId, collectionId, state, getChanges,
                requestedAnnotations, ct);
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

    // SyncKey=0: allocate state, return new key with no items.
    // Watermark=-1 is a sentinel meaning "first real sync pending".
    private async Task<SyncCollection> InitialSyncAsync(
        string userId, string deviceId, string collectionId, SyncState? state, CancellationToken ct)
    {
        if (state is null)
        {
            state = new SyncState { UserId = userId, DeviceId = deviceId, CollectionId = collectionId };
            _db.SyncStates.Add(state);
        }
        state.SyncKey = "1";
        state.Watermark = -1;
        state.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new SyncCollection { CollectionId = collectionId, SyncKey = "1", Status = 1 };
    }

    private async Task<SyncCollection> IncrementalSyncAsync(
        string userId, string collectionId, SyncState state, bool getChanges,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        // Watermark=-1: first real sync — enumerate all live items directly.
        if (state.Watermark == -1)
        {
            long tip = await _db.ItemEvents
                .Where(e => e.UserId == userId && e.CollectionId == collectionId)
                .MaxAsync(e => (long?)e.Id, ct) ?? 0;

            var all = await _db.Items
                .Where(i => i.UserId == userId && i.CollectionId == collectionId && i.DeletedAt == null)
                .ToListAsync(ct);

            await LoadContactAnnotationsAsync(all, requestedAnnotations, ct);

            state.SyncKey = SyncEngine.NextSyncKey(state.SyncKey);
            state.Watermark = tip;
            state.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var commands = all.Count > 0 ? new SyncCommands
            {
                Add = all.Select(i => new SyncAdd { ServerId = i.ServerId, ApplicationData = Serialize(i, requestedAnnotations) }).ToList(),
            } : null;

            return new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, Status = 1, Commands = commands };
        }

        if (!getChanges)
        {
            state.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, Status = 1 };
        }

        var events = await _db.ItemEvents
            .Where(e => e.UserId == userId && e.CollectionId == collectionId && e.Id > state.Watermark)
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            state.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return new SyncCollection { CollectionId = collectionId, SyncKey = state.SyncKey, Status = 1 };
        }

        var delta = SyncEngine.Collapse(events);
        var ids = delta.Added.Concat(delta.Updated).ToList();
        var itemList = await _db.Items
                .Where(i => i.UserId == userId && ids.Contains(i.ServerId))
                .ToListAsync(ct);

        await LoadContactAnnotationsAsync(itemList, requestedAnnotations, ct);

        var items = itemList.ToDictionary(i => i.ServerId);

        var cmds = new SyncCommands
        {
            Add = delta.Added.Where(items.ContainsKey)
                .Select(id => new SyncAdd { ServerId = id, ApplicationData = Serialize(items[id], requestedAnnotations) }).ToList(),
            Change = delta.Updated.Where(items.ContainsKey)
                .Select(id => new SyncChange { ServerId = id, ApplicationData = Serialize(items[id], requestedAnnotations) }).ToList(),
            Delete = delta.Deleted.Select(id => new SyncItemRef { ServerId = id }).ToList(),
        };

        state.SyncKey = SyncEngine.NextSyncKey(state.SyncKey);
        state.Watermark = delta.Watermark;
        state.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        bool hasChanges = cmds.Add.Count + cmds.Change.Count + cmds.Delete.Count > 0;
        return new SyncCollection
        {
            CollectionId = collectionId,
            SyncKey = state.SyncKey,
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
            if (!await _users.DeleteItemAsync(userId, delete.ServerId, ct))
                responses.Change.Add(new SyncResponseChange { ServerId = delete.ServerId, Status = 8 });
        }

        bool any = responses.Add.Count + responses.Change.Count + responses.Fetch.Count > 0;
        return any ? responses : null;
    }

    private async Task<(string? serverId, int status)> HandleAddAsync(
        string userId, string collectionId, string itemClass, ApplicationData? appData, CancellationToken ct)
    {
        var item = CreateItem(userId, collectionId, itemClass, appData);
        if (item is null) return (null, 6);
        var serverId = await _users.AddItemAsync(item, ct);
        return (serverId, 1);
    }

    private async Task<int> HandleChangeAsync(
        string userId, string itemClass, SyncChange change, CancellationToken ct)
    {
        bool ok = await _users.UpdateItemAsync(userId, change.ServerId,
            item => ApplyApplicationData(item, itemClass, change.ApplicationData), ct);
        return ok ? 1 : 8;
    }

    private static Item? CreateItem(string userId, string collectionId, string itemClass, ApplicationData? appData) =>
        itemClass switch
        {
            "Contact" => DeserializeAppData<ContactData>(appData)?.ToEntity(userId, collectionId),
            "Calendar" => DeserializeAppData<CalendarData>(appData)?.ToEntity(userId, collectionId),
            _ => null,
        };

    private static void ApplyApplicationData(Item item, string itemClass, ApplicationData? appData)
    {
        switch (itemClass)
        {
            case "Contact" when item is ContactItem c:
                var cd = DeserializeAppData<ContactData>(appData);
                if (cd is not null) ApplyContactData(c, cd);
                break;
            case "Calendar" when item is CalendarItem cal:
                var calData = DeserializeAppData<CalendarData>(appData);
                if (calData is not null) ApplyCalendarData(cal, calData);
                break;
        }
    }

    // ── Serialization ─────────────────────────────────────────────────────────

    private static ApplicationData? Serialize(Item item, IReadOnlySet<string>? requestedAnnotations)
    {
        var appData = item switch
        {
            ContactItem c   => DtoToApplicationData(ContactData.CreateFrom(c)),
            CalendarItem cal => DtoToApplicationData(CalendarData.CreateFrom(cal)),
            _ => null,
        };

        // Append <live:Annotations> to ApplicationData when the client subscribed and
        // this contact has annotation data stored.
        if (appData is not null
            && requestedAnnotations is { Count: > 0 }
            && item is ContactItem contact
            && contact.Annotation?.BuildAnnotations(requestedAnnotations) is { } annotations)
        {
            appData.Elements.Add(BuildAnnotationsElement(annotations));
        }

        return appData;
    }

    // Build a <live:Annotations> XmlElement from a populated Annotations model.
    private static XmlElement BuildAnnotationsElement(Annotations annotations)
    {
        var doc = new XmlDocument();
        var root = doc.CreateElement("Annotations", Constants.WindowsLive);
        foreach (var ann in annotations.Items)
        {
            var annEl  = doc.CreateElement("Annotation", Constants.WindowsLive);
            var nameEl = doc.CreateElement("Name",       Constants.WindowsLive);
            nameEl.InnerText = ann.Name;
            annEl.AppendChild(nameEl);
            if (ann.Value is not null)
            {
                var valEl = doc.CreateElement("Value", Constants.WindowsLive);
                valEl.InnerText = ann.Value;
                annEl.AppendChild(valEl);
            }
            root.AppendChild(annEl);
        }
        return root;
    }

    // Load ContactAnnotation rows for any ContactItems in the list, but only when
    // the client actually subscribed to annotations (avoids the extra query otherwise).
    private async Task LoadContactAnnotationsAsync(
        List<Item> items, IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        if (requestedAnnotations is not { Count: > 0 }) return;

        var contactIds = items.OfType<ContactItem>().Select(c => c.Id).ToList();
        if (contactIds.Count == 0) return;

        var annotations = await _db.ContactAnnotations
            .Where(a => contactIds.Contains(a.ContactItemId))
            .ToDictionaryAsync(a => a.ContactItemId, ct);

        foreach (var contact in items.OfType<ContactItem>())
            contact.Annotation = annotations.GetValueOrDefault(contact.Id);
    }

    private static void ApplyContactData(ContactItem c, ContactData data)
    {
        c.FirstName = data.FirstName; c.MiddleName = data.MiddleName;
        c.LastName = data.LastName; c.Title = data.Title;
        c.Suffix = data.Suffix; c.FileAs = data.FileAs;
        c.Alias = data.Alias; c.NickName = data.NickName;
        c.YomiFirstName = data.YomiFirstName; c.YomiLastName = data.YomiLastName;
        c.YomiCompanyName = data.YomiCompanyName;
        c.CompanyName = data.CompanyName; c.Department = data.Department;
        c.JobTitle = data.JobTitle; c.OfficeLocation = data.OfficeLocation;
        c.AccountName = data.AccountName; c.ManagerName = data.ManagerName;
        c.CustomerId = data.CustomerId; c.GovernmentId = data.GovernmentId;
        c.AssistantName = data.AssistantName;
        c.Email1Address = data.Email1Address; c.Email2Address = data.Email2Address;
        c.Email3Address = data.Email3Address;
        c.BusinessPhoneNumber = data.BusinessPhoneNumber;
        c.Business2PhoneNumber = data.Business2PhoneNumber;
        c.BusinessFaxNumber = data.BusinessFaxNumber;
        c.HomePhoneNumber = data.HomePhoneNumber;
        c.Home2PhoneNumber = data.Home2PhoneNumber;
        c.HomeFaxNumber = data.HomeFaxNumber;
        c.MobilePhoneNumber = data.MobilePhoneNumber;
        c.CarPhoneNumber = data.CarPhoneNumber;
        c.PagerNumber = data.PagerNumber; c.RadioPhoneNumber = data.RadioPhoneNumber;
        c.AssistantPhoneNumber = data.AssistantPhoneNumber;
        c.CompanyMainPhone = data.CompanyMainPhone;
        c.MMS = data.MMS;
        c.IMAddress = data.IMAddress; c.IMAddress2 = data.IMAddress2;
        c.IMAddress3 = data.IMAddress3;
        c.BusinessAddressStreet = data.BusinessAddressStreet;
        c.BusinessAddressCity = data.BusinessAddressCity;
        c.BusinessAddressState = data.BusinessAddressState;
        c.BusinessAddressPostalCode = data.BusinessAddressPostalCode;
        c.BusinessAddressCountry = data.BusinessAddressCountry;
        c.HomeAddressStreet = data.HomeAddressStreet;
        c.HomeAddressCity = data.HomeAddressCity;
        c.HomeAddressState = data.HomeAddressState;
        c.HomeAddressPostalCode = data.HomeAddressPostalCode;
        c.HomeAddressCountry = data.HomeAddressCountry;
        c.OtherAddressStreet = data.OtherAddressStreet;
        c.OtherAddressCity = data.OtherAddressCity;
        c.OtherAddressState = data.OtherAddressState;
        c.OtherAddressPostalCode = data.OtherAddressPostalCode;
        c.OtherAddressCountry = data.OtherAddressCountry;
        c.Spouse = data.Spouse; c.WebPage = data.WebPage;
        c.Birthday = data.Birthday; c.Anniversary = data.Anniversary;
        c.Picture = data.Picture;
        c.Notes = data.Body?.Data ?? data.BodyLegacy;
    }

    private static void ApplyCalendarData(CalendarItem cal, CalendarData data)
    {
        cal.Timezone = data.Timezone;
        cal.StartTime = data.StartTime; cal.EndTime = data.EndTime;
        cal.DtStamp = data.DtStamp; cal.Uid = data.Uid;
        cal.Subject = data.Subject; cal.Location = data.Location;
        cal.Reminder = data.Reminder;
        cal.AllDayEvent = data.AllDayEvent switch { 1 => true, 0 => false, _ => null };
        cal.BusyStatus = data.BusyStatus; cal.Sensitivity = data.Sensitivity;
        cal.MeetingStatus = data.MeetingStatus;
        cal.OrganizerName = data.OrganizerName; cal.OrganizerEmail = data.OrganizerEmail;
        cal.ResponseType = data.ResponseType;
        cal.ResponseRequested = data.ResponseRequested switch { 1 => true, 0 => false, _ => null };
        cal.DisallowNewTimeProposal = data.DisallowNewTimeProposal switch { 1 => true, 0 => false, _ => null };
        cal.Notes = data.Body?.Data ?? data.BodyLegacy;
        cal.BodyTruncated = data.BodyTruncated switch { 1 => true, 0 => false, _ => null };
        if (data.Recurrence is { } r)
        {
            cal.RecurrenceType = r.Type; cal.RecurrenceOccurrences = r.Occurrences;
            cal.RecurrenceInterval = r.Interval; cal.RecurrenceWeekOfMonth = r.WeekOfMonth;
            cal.RecurrenceDayOfWeek = r.DayOfWeek; cal.RecurrenceMonthOfYear = r.MonthOfYear;
            cal.RecurrenceDayOfMonth = r.DayOfMonth; cal.RecurrenceCalendarType = r.CalendarType;
            cal.RecurrenceIsLeapMonth = r.IsLeapMonth switch { 1 => true, 0 => false, _ => null };
            cal.RecurrenceFirstDayOfWeek = r.FirstDayOfWeek;
        }
    }

    private async Task<string> GetItemClassAsync(string userId, string collectionId, CancellationToken ct)
    {
        var folder = await _db.Folders.FirstOrDefaultAsync(f => f.UserId == userId && f.Id == collectionId, ct);
        return folder?.Type switch
        {
            FolderType.CalendarDefault or FolderType.Calendar => "Calendar",
            FolderType.ContactsDefault or FolderType.Contacts => "Contact",
            FolderType.TasksDefault or FolderType.Task => "Task",
            _ => "Email",
        };
    }

    // ── XML round-trip helpers ────────────────────────────────────────────────

    // Serialize a typed DTO to an ApplicationData bag by extracting child elements
    // from a temporary XmlDocument. Namespace URIs are preserved so ASWBXML can
    // select the correct code page (e.g. "Contacts" → code page 1).
    // xsi:nil elements (XmlSerializer's encoding of null Nullable<T> fields) are
    // stripped — ASWBXML has no code page for the xsi namespace and produces
    // invalid bytes for attribute-bearing elements.
    private static ApplicationData DtoToApplicationData<T>(T dto) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        var doc = new XmlDocument();
        using var sw = new StringWriter();
        serializer.Serialize(sw, dto);
        doc.LoadXml(sw.ToString());
        StripNilElements(doc.DocumentElement!);
        var appData = new ApplicationData();
        foreach (XmlNode child in doc.DocumentElement!.ChildNodes)
            if (child is XmlElement el)
                appData.Elements.Add(el);
        return appData;
    }

    // Recursively remove any element that carries xsi:nil="true".
    // These are emitted by XmlSerializer for null Nullable<T> properties and are
    // semantically equivalent to an absent element in EAS — they carry no data.
    private static void StripNilElements(XmlNode node)
    {
        const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";
        List<XmlNode>? remove = null;
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is XmlElement el && el.GetAttribute("nil", Xsi) == "true")
                (remove ??= []).Add(el);
            else
                StripNilElements(child);
        }
        if (remove is not null)
            foreach (var el in remove)
                node.RemoveChild(el);
    }

    // Reconstruct a typed DTO from an ApplicationData bag (client-supplied elements).
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
