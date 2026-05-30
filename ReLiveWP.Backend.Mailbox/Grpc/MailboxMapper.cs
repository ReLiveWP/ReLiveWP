using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using Proto = ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Grpc;

// Entity → proto and proto → entity conversions.
// Entity optional numerics (byte?, ushort?, uint?, int?, long?, bool?) must be set
// procedurally (not in object initializers) so that proto3 HasX semantics are preserved:
// leaving a field unset means HasX=false, which callers use to distinguish "not present"
// from the zero-value (e.g. BusyStatus=0 means Free, not absent).
//
// optional Timestamp fields are nullable reference types (Timestamp?) — check != null,
// not HasX.
public static class MailboxMapper
{
    // ── Timestamps ────────────────────────────────────────────────────────────

    public static Timestamp? ToProtoTs(DateTime? dt) =>
        dt.HasValue ? Timestamp.FromDateTime(DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)) : null;

    public static DateTime? FromProtoTs(Timestamp? ts) => ts?.ToDateTime();

    // ── FolderType ────────────────────────────────────────────────────────────

    public static Proto.FolderType ToProto(DbFolderType t) => t switch
    {
        DbFolderType.Generic => Proto.FolderType.Generic,
        DbFolderType.InboxDefault => Proto.FolderType.InboxDefault,
        DbFolderType.DraftsDefault => Proto.FolderType.DraftsDefault,
        DbFolderType.DeletedItemsDefault => Proto.FolderType.DeletedItemsDefault,
        DbFolderType.SentItemsDefault => Proto.FolderType.SentItemsDefault,
        DbFolderType.OutboxDefault => Proto.FolderType.OutboxDefault,
        DbFolderType.TasksDefault => Proto.FolderType.TasksDefault,
        DbFolderType.CalendarDefault => Proto.FolderType.CalendarDefault,
        DbFolderType.ContactsDefault => Proto.FolderType.ContactsDefault,
        DbFolderType.NotesDefault => Proto.FolderType.NotesDefault,
        DbFolderType.JournalDefault => Proto.FolderType.JournalDefault,
        DbFolderType.Mail => Proto.FolderType.Mail,
        DbFolderType.Calendar => Proto.FolderType.Calendar,
        DbFolderType.Contacts => Proto.FolderType.Contacts,
        DbFolderType.Task => Proto.FolderType.Task,
        DbFolderType.Journal => Proto.FolderType.Journal,
        DbFolderType.Notes => Proto.FolderType.Notes,
        DbFolderType.RecipientInformationCache => Proto.FolderType.RecipientInformationCache,
        DbFolderType.MeContact => Proto.FolderType.MeContact,
        _ => Proto.FolderType.Unknown,
    };

    public static DbFolderType FromProto(Proto.FolderType t) => t switch
    {
        Proto.FolderType.Generic => DbFolderType.Generic,
        Proto.FolderType.InboxDefault => DbFolderType.InboxDefault,
        Proto.FolderType.DraftsDefault => DbFolderType.DraftsDefault,
        Proto.FolderType.DeletedItemsDefault => DbFolderType.DeletedItemsDefault,
        Proto.FolderType.SentItemsDefault => DbFolderType.SentItemsDefault,
        Proto.FolderType.OutboxDefault => DbFolderType.OutboxDefault,
        Proto.FolderType.TasksDefault => DbFolderType.TasksDefault,
        Proto.FolderType.CalendarDefault => DbFolderType.CalendarDefault,
        Proto.FolderType.ContactsDefault => DbFolderType.ContactsDefault,
        Proto.FolderType.NotesDefault => DbFolderType.NotesDefault,
        Proto.FolderType.JournalDefault => DbFolderType.JournalDefault,
        Proto.FolderType.Mail => DbFolderType.Mail,
        Proto.FolderType.Calendar => DbFolderType.Calendar,
        Proto.FolderType.Contacts => DbFolderType.Contacts,
        Proto.FolderType.Task => DbFolderType.Task,
        Proto.FolderType.Journal => DbFolderType.Journal,
        Proto.FolderType.Notes => DbFolderType.Notes,
        Proto.FolderType.RecipientInformationCache => DbFolderType.RecipientInformationCache,
        Proto.FolderType.MeContact => DbFolderType.MeContact,
        _ => DbFolderType.Unknown,
    };

    // ── ChangeEventType ───────────────────────────────────────────────────────

    public static Proto.ChangeEventType ToProto(DbChangeEventType t) => t switch
    {
        DbChangeEventType.Update => Proto.ChangeEventType.Update,
        DbChangeEventType.Delete => Proto.ChangeEventType.Delete,
        _ => Proto.ChangeEventType.Add,
    };

    // ── Folder ────────────────────────────────────────────────────────────────

    public static Proto.Folder ToProto(DbFolder f)
    {
        var p = new Proto.Folder
        {
            Id = f.Id,
            UserId = f.UserId,
            ParentServerId = f.ParentServerId,
            DisplayName = f.DisplayName,
            Type = ToProto(f.Type),
            CreatedAt = ToProtoTs(f.CreatedAt),
            DeletedAt = ToProtoTs(f.DeletedAt),
            IsHidden = f.IsHidden,
        };
        if (f.SourceId is not null) p.SourceId = f.SourceId;
        if (f.AccountName is not null) p.AccountName = f.AccountName;
        return p;
    }

    public static DbFolder ToEntity(Proto.CreateFolderRequest r, string serverId) => new()
    {
        Id = serverId,
        UserId = r.UserId,
        ParentServerId = string.IsNullOrEmpty(r.ParentServerId) ? "0" : r.ParentServerId,
        DisplayName = r.DisplayName,
        Type = FromProto(r.Type),
        SourceId = r.HasSourceId ? r.SourceId : null,
        AccountName = r.HasAccountName ? r.AccountName : null,
        IsHidden = r.IsHidden,
        CreatedAt = DateTime.UtcNow,
    };

    // ── FolderEvent ───────────────────────────────────────────────────────────

    public static Proto.FolderEvent ToProto(DbFolderEvent e)
    {
        var p = new Proto.FolderEvent
        {
            Id = e.Id,
            UserId = e.UserId,
            EventType = ToProto(e.EventType),
            ServerId = e.ServerId,
            OccurredAt = Timestamp.FromDateTime(DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc)),
        };
        if (e.ParentServerId is not null) p.ParentServerId = e.ParentServerId;
        if (e.DisplayName is not null) p.DisplayName = e.DisplayName;
        if (e.FolderType.HasValue) p.FolderType = ToProto(e.FolderType.Value);
        return p;
    }

    // ── ItemEvent ─────────────────────────────────────────────────────────────

    public static Proto.ItemEvent ToProto(DbItemEvent e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        CollectionId = e.CollectionId,
        EventType = ToProto(e.EventType),
        ServerId = e.ServerId,
        OccurredAt = Timestamp.FromDateTime(DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc)),
    };

    // ── SyncState ─────────────────────────────────────────────────────────────

    public static Proto.SyncState ToProto(DbSyncState s)
    {
        var p = new Proto.SyncState
        {
            UserId = s.UserId,
            DeviceId = s.DeviceId,
            CollectionId = s.CollectionId,
            SyncKey = s.SyncKey,
            Watermark = s.Watermark,
            LastSeenAt = Timestamp.FromDateTime(DateTime.SpecifyKind(s.LastSeenAt, DateTimeKind.Utc)),
        };
        if (s.CachedAnnotationNames is not null) p.CachedAnnotationNames = s.CachedAnnotationNames;
        return p;
    }

    // ── DeviceInfo ────────────────────────────────────────────────────────────

    public static Proto.DeviceInfo ToProto(DbDeviceInfo d)
    {
        var p = new Proto.DeviceInfo
        {
            UserId = d.UserId,
            DeviceId = d.DeviceId,
            UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(d.UpdatedAt, DateTimeKind.Utc)),
        };
        if (d.Model is not null) p.Model = d.Model;
        if (d.IMEI is not null) p.Imei = d.IMEI;
        if (d.FriendlyName is not null) p.FriendlyName = d.FriendlyName;
        if (d.OS is not null) p.Os = d.OS;
        if (d.OSLanguage is not null) p.OsLanguage = d.OSLanguage;
        if (d.PhoneNumber is not null) p.PhoneNumber = d.PhoneNumber;
        if (d.UserAgent is not null) p.UserAgent = d.UserAgent;
        if (d.EnableOutboundSMS.HasValue) p.EnableOutboundSms = d.EnableOutboundSMS.Value;
        if (d.MobileOperator is not null) p.MobileOperator = d.MobileOperator;
        return p;
    }

    // ── Item (envelope) ───────────────────────────────────────────────────────

    public static Proto.Item ToProto(DbItem item)
    {
        var p = new Proto.Item
        {
            Id = item.Id,
            UserId = item.UserId,
            CollectionId = item.CollectionId,
            ServerId = item.ServerId,
            CreatedAt = ToProtoTs(item.CreatedAt),
            DeletedAt = ToProtoTs(item.DeletedAt),
        };

        switch (item)
        {
            case DbContactItem c: p.Contact = ToProto(c); break;
            case DbCalendarItem cal: p.Calendar = ToProto(cal); break;
            case DbTask: p.Task = new Proto.TaskItem(); break;
            case DbEmail: p.Email = new Proto.EmailItem(); break;
            default: throw new InvalidOperationException($"Unknown item type {item.GetType().Name}");
        }

        return p;
    }

    // ── ContactItem ───────────────────────────────────────────────────────────

    public static Proto.ContactItem ToProto(DbContactItem c)
    {
        var p = new Proto.ContactItem();

        if (c.FirstName is not null) p.FirstName = c.FirstName;
        if (c.MiddleName is not null) p.MiddleName = c.MiddleName;
        if (c.LastName is not null) p.LastName = c.LastName;
        if (c.Title is not null) p.Title = c.Title;
        if (c.Suffix is not null) p.Suffix = c.Suffix;
        if (c.FileAs is not null) p.FileAs = c.FileAs;
        if (c.Alias is not null) p.Alias = c.Alias;
        if (c.NickName is not null) p.NickName = c.NickName;
        if (c.YomiFirstName is not null) p.YomiFirstName = c.YomiFirstName;
        if (c.YomiLastName is not null) p.YomiLastName = c.YomiLastName;
        if (c.YomiCompanyName is not null) p.YomiCompanyName = c.YomiCompanyName;
        if (c.CompanyName is not null) p.CompanyName = c.CompanyName;
        if (c.Department is not null) p.Department = c.Department;
        if (c.JobTitle is not null) p.JobTitle = c.JobTitle;
        if (c.OfficeLocation is not null) p.OfficeLocation = c.OfficeLocation;
        if (c.AccountName is not null) p.AccountName = c.AccountName;
        if (c.ManagerName is not null) p.ManagerName = c.ManagerName;
        if (c.CustomerId is not null) p.CustomerId = c.CustomerId;
        if (c.GovernmentId is not null) p.GovernmentId = c.GovernmentId;
        if (c.AssistantName is not null) p.AssistantName = c.AssistantName;
        if (c.Email1Address is not null) p.Email1Address = c.Email1Address;
        if (c.Email2Address is not null) p.Email2Address = c.Email2Address;
        if (c.Email3Address is not null) p.Email3Address = c.Email3Address;
        if (c.BusinessPhoneNumber is not null) p.BusinessPhoneNumber = c.BusinessPhoneNumber;
        if (c.Business2PhoneNumber is not null) p.Business2PhoneNumber = c.Business2PhoneNumber;
        if (c.BusinessFaxNumber is not null) p.BusinessFaxNumber = c.BusinessFaxNumber;
        if (c.HomePhoneNumber is not null) p.HomePhoneNumber = c.HomePhoneNumber;
        if (c.Home2PhoneNumber is not null) p.Home2PhoneNumber = c.Home2PhoneNumber;
        if (c.HomeFaxNumber is not null) p.HomeFaxNumber = c.HomeFaxNumber;
        if (c.MobilePhoneNumber is not null) p.MobilePhoneNumber = c.MobilePhoneNumber;
        if (c.CarPhoneNumber is not null) p.CarPhoneNumber = c.CarPhoneNumber;
        if (c.PagerNumber is not null) p.PagerNumber = c.PagerNumber;
        if (c.RadioPhoneNumber is not null) p.RadioPhoneNumber = c.RadioPhoneNumber;
        if (c.AssistantPhoneNumber is not null) p.AssistantPhoneNumber = c.AssistantPhoneNumber;
        if (c.CompanyMainPhone is not null) p.CompanyMainPhone = c.CompanyMainPhone;
        if (c.MMS is not null) p.Mms = c.MMS;
        if (c.IMAddress is not null) p.ImAddress = c.IMAddress;
        if (c.IMAddress2 is not null) p.ImAddress2 = c.IMAddress2;
        if (c.IMAddress3 is not null) p.ImAddress3 = c.IMAddress3;
        if (c.BusinessAddressStreet is not null) p.BusinessAddressStreet = c.BusinessAddressStreet;
        if (c.BusinessAddressCity is not null) p.BusinessAddressCity = c.BusinessAddressCity;
        if (c.BusinessAddressState is not null) p.BusinessAddressState = c.BusinessAddressState;
        if (c.BusinessAddressPostalCode is not null) p.BusinessAddressPostalCode = c.BusinessAddressPostalCode;
        if (c.BusinessAddressCountry is not null) p.BusinessAddressCountry = c.BusinessAddressCountry;
        if (c.HomeAddressStreet is not null) p.HomeAddressStreet = c.HomeAddressStreet;
        if (c.HomeAddressCity is not null) p.HomeAddressCity = c.HomeAddressCity;
        if (c.HomeAddressState is not null) p.HomeAddressState = c.HomeAddressState;
        if (c.HomeAddressPostalCode is not null) p.HomeAddressPostalCode = c.HomeAddressPostalCode;
        if (c.HomeAddressCountry is not null) p.HomeAddressCountry = c.HomeAddressCountry;
        if (c.OtherAddressStreet is not null) p.OtherAddressStreet = c.OtherAddressStreet;
        if (c.OtherAddressCity is not null) p.OtherAddressCity = c.OtherAddressCity;
        if (c.OtherAddressState is not null) p.OtherAddressState = c.OtherAddressState;
        if (c.OtherAddressPostalCode is not null) p.OtherAddressPostalCode = c.OtherAddressPostalCode;
        if (c.OtherAddressCountry is not null) p.OtherAddressCountry = c.OtherAddressCountry;
        if (c.Spouse is not null) p.Spouse = c.Spouse;
        if (c.WebPage is not null) p.WebPage = c.WebPage;
        if (c.Notes is not null) p.Notes = c.Notes;
        if (c.Birthday.HasValue) p.Birthday = ToProtoTs(c.Birthday);
        if (c.Anniversary.HasValue) p.Anniversary = ToProtoTs(c.Anniversary);
        if (c.Picture is { Length: > 0 }) p.Picture = Google.Protobuf.ByteString.CopyFrom(c.Picture);
        if (c.WeightedRank.HasValue) p.WeightedRank = c.WeightedRank.Value;

        p.Categories.AddRange(c.Categories.Select(x => new Proto.ContactCategory
        { Id = x.Id, ContactItemId = x.ContactItemId, Name = x.Name }));
        p.Children.AddRange(c.Children.Select(x => new Proto.ContactChild
        { Id = x.Id, ContactItemId = x.ContactItemId, Name = x.Name }));

        if (c.Annotation is { } ann)
            p.Annotation = ToProto(ann);

        return p;
    }

    public static DbContactItem ToEntity(string userId, string collectionId, Proto.ContactItem p,
        Proto.ContactAnnotation? annotation = null) =>
        ApplyToEntity(new DbContactItem { UserId = userId, CollectionId = collectionId }, p, annotation);

    public static DbContactItem ApplyToEntity(DbContactItem c, Proto.ContactItem p,
        Proto.ContactAnnotation? annotation = null)
    {
        c.FirstName = p.HasFirstName ? p.FirstName : null;
        c.MiddleName = p.HasMiddleName ? p.MiddleName : null;
        c.LastName = p.HasLastName ? p.LastName : null;
        c.Title = p.HasTitle ? p.Title : null;
        c.Suffix = p.HasSuffix ? p.Suffix : null;
        c.FileAs = p.HasFileAs ? p.FileAs : null;
        c.Alias = p.HasAlias ? p.Alias : null;
        c.NickName = p.HasNickName ? p.NickName : null;
        c.YomiFirstName = p.HasYomiFirstName ? p.YomiFirstName : null;
        c.YomiLastName = p.HasYomiLastName ? p.YomiLastName : null;
        c.YomiCompanyName = p.HasYomiCompanyName ? p.YomiCompanyName : null;
        c.CompanyName = p.HasCompanyName ? p.CompanyName : null;
        c.Department = p.HasDepartment ? p.Department : null;
        c.JobTitle = p.HasJobTitle ? p.JobTitle : null;
        c.OfficeLocation = p.HasOfficeLocation ? p.OfficeLocation : null;
        c.AccountName = p.HasAccountName ? p.AccountName : null;
        c.ManagerName = p.HasManagerName ? p.ManagerName : null;
        c.CustomerId = p.HasCustomerId ? p.CustomerId : null;
        c.GovernmentId = p.HasGovernmentId ? p.GovernmentId : null;
        c.AssistantName = p.HasAssistantName ? p.AssistantName : null;
        c.Email1Address = p.HasEmail1Address ? p.Email1Address : null;
        c.Email2Address = p.HasEmail2Address ? p.Email2Address : null;
        c.Email3Address = p.HasEmail3Address ? p.Email3Address : null;
        c.BusinessPhoneNumber = p.HasBusinessPhoneNumber ? p.BusinessPhoneNumber : null;
        c.Business2PhoneNumber = p.HasBusiness2PhoneNumber ? p.Business2PhoneNumber : null;
        c.BusinessFaxNumber = p.HasBusinessFaxNumber ? p.BusinessFaxNumber : null;
        c.HomePhoneNumber = p.HasHomePhoneNumber ? p.HomePhoneNumber : null;
        c.Home2PhoneNumber = p.HasHome2PhoneNumber ? p.Home2PhoneNumber : null;
        c.HomeFaxNumber = p.HasHomeFaxNumber ? p.HomeFaxNumber : null;
        c.MobilePhoneNumber = p.HasMobilePhoneNumber ? p.MobilePhoneNumber : null;
        c.CarPhoneNumber = p.HasCarPhoneNumber ? p.CarPhoneNumber : null;
        c.PagerNumber = p.HasPagerNumber ? p.PagerNumber : null;
        c.RadioPhoneNumber = p.HasRadioPhoneNumber ? p.RadioPhoneNumber : null;
        c.AssistantPhoneNumber = p.HasAssistantPhoneNumber ? p.AssistantPhoneNumber : null;
        c.CompanyMainPhone = p.HasCompanyMainPhone ? p.CompanyMainPhone : null;
        c.MMS = p.HasMms ? p.Mms : null;
        c.IMAddress = p.HasImAddress ? p.ImAddress : null;
        c.IMAddress2 = p.HasImAddress2 ? p.ImAddress2 : null;
        c.IMAddress3 = p.HasImAddress3 ? p.ImAddress3 : null;
        c.BusinessAddressStreet = p.HasBusinessAddressStreet ? p.BusinessAddressStreet : null;
        c.BusinessAddressCity = p.HasBusinessAddressCity ? p.BusinessAddressCity : null;
        c.BusinessAddressState = p.HasBusinessAddressState ? p.BusinessAddressState : null;
        c.BusinessAddressPostalCode = p.HasBusinessAddressPostalCode ? p.BusinessAddressPostalCode : null;
        c.BusinessAddressCountry = p.HasBusinessAddressCountry ? p.BusinessAddressCountry : null;
        c.HomeAddressStreet = p.HasHomeAddressStreet ? p.HomeAddressStreet : null;
        c.HomeAddressCity = p.HasHomeAddressCity ? p.HomeAddressCity : null;
        c.HomeAddressState = p.HasHomeAddressState ? p.HomeAddressState : null;
        c.HomeAddressPostalCode = p.HasHomeAddressPostalCode ? p.HomeAddressPostalCode : null;
        c.HomeAddressCountry = p.HasHomeAddressCountry ? p.HomeAddressCountry : null;
        c.OtherAddressStreet = p.HasOtherAddressStreet ? p.OtherAddressStreet : null;
        c.OtherAddressCity = p.HasOtherAddressCity ? p.OtherAddressCity : null;
        c.OtherAddressState = p.HasOtherAddressState ? p.OtherAddressState : null;
        c.OtherAddressPostalCode = p.HasOtherAddressPostalCode ? p.OtherAddressPostalCode : null;
        c.OtherAddressCountry = p.HasOtherAddressCountry ? p.OtherAddressCountry : null;
        c.Spouse = p.HasSpouse ? p.Spouse : null;
        c.WebPage = p.HasWebPage ? p.WebPage : null;
        c.Notes = p.HasNotes ? p.Notes : null;
        c.Birthday = p.Birthday != null ? FromProtoTs(p.Birthday) : null;
        c.Anniversary = p.Anniversary != null ? FromProtoTs(p.Anniversary) : null;
        c.Picture = p.HasPicture ? p.Picture.ToByteArray() : null;
        c.WeightedRank = p.HasWeightedRank ? p.WeightedRank : null;
        return c;
    }

    // ── ContactAnnotation ─────────────────────────────────────────────────────

    public static Proto.ContactAnnotation ToProto(DbContactAnnotation a)
    {
        var p = new Proto.ContactAnnotation { ContactItemId = a.ContactItemId };
        if (a.Cid.HasValue) p.Cid = a.Cid.Value;
        if (a.ObjectId is not null) p.ObjectId = a.ObjectId;
        if (a.WLId is not null) p.WlId = a.WLId;
        if (a.ImMri is not null) p.ImMri = a.ImMri;
        if (a.ContactType is not null) p.ContactType = a.ContactType;
        if (a.UserTileUrl is not null) p.UserTileUrl = a.UserTileUrl;
        if (a.UserTileHash is not null) p.UserTileHash = a.UserTileHash;
        if (a.TrustLevel.HasValue) p.TrustLevel = a.TrustLevel.Value;
        if (a.FavoriteOrder.HasValue) p.FavoriteOrder = a.FavoriteOrder.Value;
        return p;
    }

    public static DbContactAnnotation ToEntity(Proto.ContactAnnotation p, string contactItemId) => new()
    {
        ContactItemId = contactItemId,
        Cid = p.HasCid ? p.Cid : null,
        ObjectId = p.HasObjectId ? p.ObjectId : null,
        WLId = p.HasWlId ? p.WlId : null,
        ImMri = p.HasImMri ? p.ImMri : null,
        ContactType = p.HasContactType ? p.ContactType : null,
        UserTileUrl = p.HasUserTileUrl ? p.UserTileUrl : null,
        UserTileHash = p.HasUserTileHash ? p.UserTileHash : null,
        TrustLevel = p.HasTrustLevel ? p.TrustLevel : null,
        FavoriteOrder = p.HasFavoriteOrder ? p.FavoriteOrder : null,
    };

    // ── CalendarItem ──────────────────────────────────────────────────────────

    public static Proto.CalendarItem ToProto(DbCalendarItem cal)
    {
        var p = new Proto.CalendarItem
        {
            StartTime = ToProtoTs(cal.StartTime),
            EndTime = ToProtoTs(cal.EndTime),
            DtStamp = ToProtoTs(cal.DtStamp),
            AppointmentReplyTime = ToProtoTs(cal.AppointmentReplyTime),
            RecurrenceUntil = ToProtoTs(cal.RecurrenceUntil),
        };

        if (cal.Timezone is not null) p.Timezone = cal.Timezone;
        if (cal.Uid is not null) p.Uid = cal.Uid;
        if (cal.ClientUid is not null) p.ClientUid = cal.ClientUid;
        if (cal.Subject is not null) p.Subject = cal.Subject;
        if (cal.Location is not null) p.Location = cal.Location;
        if (cal.OrganizerName is not null) p.OrganizerName = cal.OrganizerName;
        if (cal.OrganizerEmail is not null) p.OrganizerEmail = cal.OrganizerEmail;
        if (cal.Notes is not null) p.Notes = cal.Notes;
        if (cal.BodyLegacy is not null) p.BodyLegacy = cal.BodyLegacy;
        if (cal.OnlineMeetingConfLink is not null) p.OnlineMeetingConfLink = cal.OnlineMeetingConfLink;
        if (cal.OnlineMeetingExternalLink is not null) p.OnlineMeetingExternalLink = cal.OnlineMeetingExternalLink;
        if (cal.Reminder.HasValue) p.Reminder = cal.Reminder.Value;
        if (cal.AllDayEvent.HasValue) p.AllDayEvent = cal.AllDayEvent.Value;
        if (cal.BusyStatus.HasValue) p.BusyStatus = cal.BusyStatus.Value;
        if (cal.Sensitivity.HasValue) p.Sensitivity = cal.Sensitivity.Value;
        if (cal.MeetingStatus.HasValue) p.MeetingStatus = cal.MeetingStatus.Value;
        if (cal.NativeBodyType.HasValue) p.NativeBodyType = cal.NativeBodyType.Value;
        if (cal.BodyTruncated.HasValue) p.BodyTruncated = cal.BodyTruncated.Value;
        if (cal.ResponseType.HasValue) p.ResponseType = cal.ResponseType.Value;
        if (cal.ResponseRequested.HasValue) p.ResponseRequested = cal.ResponseRequested.Value;
        if (cal.DisallowNewTimeProposal.HasValue) p.DisallowNewTimeProposal = cal.DisallowNewTimeProposal.Value;
        if (cal.RecurrenceType.HasValue) p.RecurrenceType = cal.RecurrenceType.Value;
        if (cal.RecurrenceOccurrences.HasValue) p.RecurrenceOccurrences = cal.RecurrenceOccurrences.Value;
        if (cal.RecurrenceInterval.HasValue) p.RecurrenceInterval = cal.RecurrenceInterval.Value;
        if (cal.RecurrenceWeekOfMonth.HasValue) p.RecurrenceWeekOfMonth = cal.RecurrenceWeekOfMonth.Value;
        if (cal.RecurrenceDayOfWeek.HasValue) p.RecurrenceDayOfWeek = cal.RecurrenceDayOfWeek.Value;
        if (cal.RecurrenceMonthOfYear.HasValue) p.RecurrenceMonthOfYear = cal.RecurrenceMonthOfYear.Value;
        if (cal.RecurrenceDayOfMonth.HasValue) p.RecurrenceDayOfMonth = cal.RecurrenceDayOfMonth.Value;
        if (cal.RecurrenceCalendarType.HasValue) p.RecurrenceCalendarType = cal.RecurrenceCalendarType.Value;
        if (cal.RecurrenceIsLeapMonth.HasValue) p.RecurrenceIsLeapMonth = cal.RecurrenceIsLeapMonth.Value;
        if (cal.RecurrenceFirstDayOfWeek.HasValue) p.RecurrenceFirstDayOfWeek = cal.RecurrenceFirstDayOfWeek.Value;

        p.Attendees.AddRange(cal.Attendees.Select(ToProto));
        p.Categories.AddRange(cal.Categories.Select(c => new Proto.CalendarCategory
        { Id = c.Id, CalendarItemId = c.CalendarItemId, Category = c.Category }));
        p.Exceptions.AddRange(cal.Exceptions.Select(ToProto));

        return p;
    }

    private static Proto.CalendarAttendee ToProto(DbCalendarAttendee a)
    {
        var p = new Proto.CalendarAttendee
        { Id = a.Id, CalendarItemId = a.CalendarItemId, Email = a.Email, Name = a.Name };
        if (a.AttendeeStatus.HasValue) p.AttendeeStatus = a.AttendeeStatus.Value;
        if (a.AttendeeType.HasValue) p.AttendeeType = a.AttendeeType.Value;
        return p;
    }

    private static Proto.CalendarException ToProto(DbCalendarException ex)
    {
        var p = new Proto.CalendarException
        {
            Id = ex.Id,
            CalendarItemId = ex.CalendarItemId,
            StartTime = ToProtoTs(ex.StartTime),
            EndTime = ToProtoTs(ex.EndTime),
            DtStamp = ToProtoTs(ex.DtStamp),
            AppointmentReplyTime = ToProtoTs(ex.AppointmentReplyTime),
        };
        if (ex.Deleted.HasValue) p.Deleted = ex.Deleted.Value;
        if (ex.ExceptionStartTime is not null) p.ExceptionStartTime = ex.ExceptionStartTime;
        if (ex.InstanceId is not null) p.InstanceId = ex.InstanceId;
        if (ex.Subject is not null) p.Subject = ex.Subject;
        if (ex.Location is not null) p.Location = ex.Location;
        if (ex.Notes is not null) p.Notes = ex.Notes;
        if (ex.BodyLegacy is not null) p.BodyLegacy = ex.BodyLegacy;
        if (ex.OnlineMeetingConfLink is not null) p.OnlineMeetingConfLink = ex.OnlineMeetingConfLink;
        if (ex.OnlineMeetingExternalLink is not null) p.OnlineMeetingExternalLink = ex.OnlineMeetingExternalLink;
        if (ex.Sensitivity.HasValue) p.Sensitivity = ex.Sensitivity.Value;
        if (ex.BusyStatus.HasValue) p.BusyStatus = ex.BusyStatus.Value;
        if (ex.AllDayEvent.HasValue) p.AllDayEvent = ex.AllDayEvent.Value;
        if (ex.Reminder.HasValue) p.Reminder = ex.Reminder.Value;
        if (ex.MeetingStatus.HasValue) p.MeetingStatus = ex.MeetingStatus.Value;
        if (ex.ResponseType.HasValue) p.ResponseType = ex.ResponseType.Value;

        p.Attendees.AddRange(ex.Attendees.Select(a =>
        {
            var ap = new Proto.CalendarExceptionAttendee
            { Id = a.Id, CalendarExceptionId = a.CalendarExceptionId, Email = a.Email, Name = a.Name };
            if (a.AttendeeStatus.HasValue) ap.AttendeeStatus = a.AttendeeStatus.Value;
            if (a.AttendeeType.HasValue) ap.AttendeeType = a.AttendeeType.Value;
            return ap;
        }));
        p.Categories.AddRange(ex.Categories.Select(c => new Proto.CalendarExceptionCategory
        { Id = c.Id, CalendarExceptionId = c.CalendarExceptionId, Category = c.Category }));

        return p;
    }

    public static DbCalendarItem ToEntity(string userId, string collectionId, Proto.CalendarItem p) =>
        ApplyToEntity(new DbCalendarItem { UserId = userId, CollectionId = collectionId }, p);

    public static DbCalendarItem ApplyToEntity(DbCalendarItem cal, Proto.CalendarItem p)
    {
        cal.Timezone = p.HasTimezone ? p.Timezone : null;
        cal.StartTime = p.StartTime != null ? FromProtoTs(p.StartTime) : null;
        cal.EndTime = p.EndTime != null ? FromProtoTs(p.EndTime) : null;
        cal.DtStamp = p.DtStamp != null ? FromProtoTs(p.DtStamp) : null;
        cal.Uid = p.HasUid ? p.Uid : null;
        cal.ClientUid = p.HasClientUid ? p.ClientUid : null;
        cal.Subject = p.HasSubject ? p.Subject : null;
        cal.Location = p.HasLocation ? p.Location : null;
        cal.OrganizerName = p.HasOrganizerName ? p.OrganizerName : null;
        cal.OrganizerEmail = p.HasOrganizerEmail ? p.OrganizerEmail : null;
        cal.Notes = p.HasNotes ? p.Notes : null;
        cal.BodyLegacy = p.HasBodyLegacy ? p.BodyLegacy : null;
        cal.OnlineMeetingConfLink = p.HasOnlineMeetingConfLink ? p.OnlineMeetingConfLink : null;
        cal.OnlineMeetingExternalLink = p.HasOnlineMeetingExternalLink ? p.OnlineMeetingExternalLink : null;
        cal.Reminder = p.HasReminder ? p.Reminder : null;
        cal.AllDayEvent = p.HasAllDayEvent ? p.AllDayEvent : null;
        cal.BusyStatus = p.HasBusyStatus ? (byte)p.BusyStatus : null;
        cal.Sensitivity = p.HasSensitivity ? (byte)p.Sensitivity : null;
        cal.MeetingStatus = p.HasMeetingStatus ? (byte)p.MeetingStatus : null;
        cal.NativeBodyType = p.HasNativeBodyType ? (byte)p.NativeBodyType : null;
        cal.BodyTruncated = p.HasBodyTruncated ? p.BodyTruncated : null;
        cal.AppointmentReplyTime = p.AppointmentReplyTime != null ? FromProtoTs(p.AppointmentReplyTime) : null;
        cal.ResponseType = p.HasResponseType ? p.ResponseType : null;
        cal.ResponseRequested = p.HasResponseRequested ? p.ResponseRequested : null;
        cal.DisallowNewTimeProposal = p.HasDisallowNewTimeProposal ? p.DisallowNewTimeProposal : null;
        cal.RecurrenceType = p.HasRecurrenceType ? (byte)p.RecurrenceType : null;
        cal.RecurrenceOccurrences = p.HasRecurrenceOccurrences ? (ushort)p.RecurrenceOccurrences : null;
        cal.RecurrenceInterval = p.HasRecurrenceInterval ? (ushort)p.RecurrenceInterval : null;
        cal.RecurrenceWeekOfMonth = p.HasRecurrenceWeekOfMonth ? (byte)p.RecurrenceWeekOfMonth : null;
        cal.RecurrenceDayOfWeek = p.HasRecurrenceDayOfWeek ? (ushort)p.RecurrenceDayOfWeek : null;
        cal.RecurrenceMonthOfYear = p.HasRecurrenceMonthOfYear ? (byte)p.RecurrenceMonthOfYear : null;
        cal.RecurrenceUntil = p.RecurrenceUntil != null ? FromProtoTs(p.RecurrenceUntil) : null;
        cal.RecurrenceDayOfMonth = p.HasRecurrenceDayOfMonth ? (byte)p.RecurrenceDayOfMonth : null;
        cal.RecurrenceCalendarType = p.HasRecurrenceCalendarType ? (byte)p.RecurrenceCalendarType : null;
        cal.RecurrenceIsLeapMonth = p.HasRecurrenceIsLeapMonth ? p.RecurrenceIsLeapMonth : null;
        cal.RecurrenceFirstDayOfWeek = p.HasRecurrenceFirstDayOfWeek ? (byte)p.RecurrenceFirstDayOfWeek : null;
        return cal;
    }
}
