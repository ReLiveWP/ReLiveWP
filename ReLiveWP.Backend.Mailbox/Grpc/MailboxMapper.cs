using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Grpc;

// entity <-> proto conversions. grpc has no nullable scalars, so null/missing fields skip the setter
// entirely; Timestamp fields are the exception and are checked with != null instead of HasX.
public static class MailboxMapper
{
    public static Timestamp? ToProtoTs(DateTime? dt) =>
        dt.HasValue ? Timestamp.FromDateTime(DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)) : null;

    public static DateTime? FromProtoTs(Timestamp? ts) => ts?.ToDateTime();

    // these enums must be kept in sync with the proto file
    public static FolderType ToProto(DbFolderType t)
        => (FolderType)t;
    public static DbFolderType FromProto(FolderType t)
        => (DbFolderType)t;
    public static ChangeEventType ToProto(DbChangeEventType t)
        => (ChangeEventType)t;

    public static Folder ToProto(DbFolder f)
    {
        var p = new Folder
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

    public static DbFolder ToEntity(CreateFolderRequest r, string serverId) => new()
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

    public static FolderEvent ToProto(DbFolderEvent e)
    {
        var p = new FolderEvent
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

    public static ItemEvent ToProto(DbItemEvent e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        CollectionId = e.CollectionId,
        EventType = ToProto(e.EventType),
        ServerId = e.ServerId,
        OccurredAt = Timestamp.FromDateTime(DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc)),
    };

    public static SyncState ToProto(DbSyncState s)
    {
        var p = new SyncState
        {
            UserId = s.UserId,
            DeviceId = s.DeviceId,
            CollectionId = s.CollectionId,
            SyncKey = s.SyncKey,
            Watermark = s.Watermark,
            LastSeenAt = Timestamp.FromDateTime(DateTime.SpecifyKind(s.LastSeenAt, DateTimeKind.Utc)),
            PreviousSyncKey = s.PreviousSyncKey,
            PreviousWatermark = s.PreviousWatermark,
        };

        if (s.CachedAnnotationNames is not null) p.CachedAnnotationNames = s.CachedAnnotationNames;
        return p;
    }

    public static DeviceInfo ToProto(DbDeviceInfo d)
    {
        var p = new DeviceInfo
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

    public static Item ToProto(DbItem item)
    {
        var p = new Item
        {
            Id = item.Id,
            UserId = item.UserId,
            CollectionId = item.CollectionId,
            ServerId = item.ServerId,
            CreatedAt = ToProtoTs(item.CreatedAt),
            DeletedAt = ToProtoTs(item.DeletedAt),
        };
        if (item.ClientId is not null) p.ClientId = item.ClientId;

        switch (item)
        {
            case DbContactItem c: p.Contact = ToProto(c); break;
            case DbCalendarItem cal: p.Calendar = ToProto(cal); break;
            case DbTask t: p.Task = ToProto(t); break;
            case DbEmail e: p.Email = ToProto(e); break;
            case DbNote n: p.Note = ToProto(n); break;
            default: throw new InvalidOperationException($"Unknown item type {item.GetType().Name}");
        }

        return p;
    }

    public static ContactItem ToProto(DbContactItem c)
    {
        var p = new ContactItem();

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

        p.Categories.AddRange(c.Categories.Select(x => new ContactCategory { Id = x.Id, ContactItemId = x.ContactItemId, Name = x.Name }));
        p.Children.AddRange(c.Children.Select(x => new ContactChild { Id = x.Id, ContactItemId = x.ContactItemId, Name = x.Name }));

        if (c.Annotation is { } ann)
            p.Annotation = ToProto(ann);

        return p;
    }

    public static DbContactItem ToEntity(string userId, string collectionId, ContactItem p,
        ContactAnnotation? annotation = null) =>
        ApplyToEntity(new DbContactItem { UserId = userId, CollectionId = collectionId }, p, annotation);

    public static DbContactItem ApplyToEntity(DbContactItem c, ContactItem p,
        ContactAnnotation? annotation = null)
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

    public static ContactAnnotation ToProto(DbContactAnnotation a)
    {
        var p = new ContactAnnotation { ContactItemId = a.ContactItemId };

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

    public static DbContactAnnotation ToEntity(ContactAnnotation p, string contactItemId) => new()
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

    public static CalendarItem ToProto(DbCalendarItem cal)
    {
        var p = new CalendarItem
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
        p.Categories.AddRange(cal.Categories.Select(c => new CalendarCategory { Id = c.Id, CalendarItemId = c.CalendarItemId, Category = c.Category }));
        p.Exceptions.AddRange(cal.Exceptions.Select(ToProto));

        return p;
    }

    private static CalendarAttendee ToProto(DbCalendarAttendee a)
    {
        var p = new CalendarAttendee
        {
            Id = a.Id,
            CalendarItemId = a.CalendarItemId,
            Email = a.Email,
            Name = a.Name
        };

        if (a.AttendeeStatus.HasValue) p.AttendeeStatus = a.AttendeeStatus.Value;
        if (a.AttendeeType.HasValue) p.AttendeeType = a.AttendeeType.Value;
        return p;
    }

    private static CalendarException ToProto(DbCalendarException ex)
    {
        var p = new CalendarException
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
            var ap = new CalendarExceptionAttendee 
            { 
                Id = a.Id, 
                CalendarExceptionId = a.CalendarExceptionId, 
                Email = a.Email, 
                Name = a.Name 
            };

            if (a.AttendeeStatus.HasValue) ap.AttendeeStatus = a.AttendeeStatus.Value;
            if (a.AttendeeType.HasValue) ap.AttendeeType = a.AttendeeType.Value;
            return ap;
        }));

        p.Categories.AddRange(ex.Categories.Select(c => new CalendarExceptionCategory { Id = c.Id, CalendarExceptionId = c.CalendarExceptionId, Category = c.Category }));

        return p;
    }

    public static DbCalendarItem ToEntity(string userId, string collectionId, CalendarItem p) =>
        ApplyToEntity(new DbCalendarItem { UserId = userId, CollectionId = collectionId }, p);

    public static DbCalendarItem ApplyToEntity(DbCalendarItem cal, CalendarItem p)
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

    public static TaskItem ToProto(DbTask t)
    {
        var p = new TaskItem
        {
            StartDate = ToProtoTs(t.StartDate),
            UtcStartDate = ToProtoTs(t.UtcStartDate),
            DueDate = ToProtoTs(t.DueDate),
            UtcDueDate = ToProtoTs(t.UtcDueDate),
            DateCompleted = ToProtoTs(t.DateCompleted),
            ReminderTime = ToProtoTs(t.ReminderTime),
            RecurrenceStart = ToProtoTs(t.RecurrenceStart),
            RecurrenceUntil = ToProtoTs(t.RecurrenceUntil),
        };

        if (t.Subject is not null) p.Subject = t.Subject;
        if (t.Notes is not null) p.Notes = t.Notes;
        if (t.Importance.HasValue) p.Importance = t.Importance.Value;
        if (t.Sensitivity.HasValue) p.Sensitivity = t.Sensitivity.Value;
        if (t.NativeBodyType.HasValue) p.NativeBodyType = t.NativeBodyType.Value;
        if (t.Complete.HasValue) p.Complete = t.Complete.Value;
        if (t.ReminderSet.HasValue) p.ReminderSet = t.ReminderSet.Value;
        if (t.RecurrenceType.HasValue) p.RecurrenceType = t.RecurrenceType.Value;
        if (t.RecurrenceOccurrences.HasValue) p.RecurrenceOccurrences = t.RecurrenceOccurrences.Value;
        if (t.RecurrenceInterval.HasValue) p.RecurrenceInterval = t.RecurrenceInterval.Value;
        if (t.RecurrenceDayOfWeek.HasValue) p.RecurrenceDayOfWeek = t.RecurrenceDayOfWeek.Value;
        if (t.RecurrenceDayOfMonth.HasValue) p.RecurrenceDayOfMonth = t.RecurrenceDayOfMonth.Value;
        if (t.RecurrenceWeekOfMonth.HasValue) p.RecurrenceWeekOfMonth = t.RecurrenceWeekOfMonth.Value;
        if (t.RecurrenceMonthOfYear.HasValue) p.RecurrenceMonthOfYear = t.RecurrenceMonthOfYear.Value;
        if (t.RecurrenceRegenerate.HasValue) p.RecurrenceRegenerate = t.RecurrenceRegenerate.Value;
        if (t.RecurrenceDeadOccur.HasValue) p.RecurrenceDeadOccur = t.RecurrenceDeadOccur.Value;
        if (t.RecurrenceCalendarType.HasValue) p.RecurrenceCalendarType = t.RecurrenceCalendarType.Value;
        if (t.RecurrenceIsLeapMonth.HasValue) p.RecurrenceIsLeapMonth = t.RecurrenceIsLeapMonth.Value;
        if (t.RecurrenceFirstDayOfWeek.HasValue) p.RecurrenceFirstDayOfWeek = t.RecurrenceFirstDayOfWeek.Value;

        return p;
    }

    public static DbTask ToEntity(string userId, string collectionId, TaskItem p) =>
        ApplyToEntity(new DbTask { UserId = userId, CollectionId = collectionId }, p);

    public static DbTask ApplyToEntity(DbTask t, TaskItem p)
    {
        t.Subject = p.HasSubject ? p.Subject : null;
        t.Notes = p.HasNotes ? p.Notes : null;
        t.Importance = p.HasImportance ? (byte)p.Importance : null;
        t.Sensitivity = p.HasSensitivity ? (byte)p.Sensitivity : null;
        t.NativeBodyType = p.HasNativeBodyType ? (byte)p.NativeBodyType : null;
        t.StartDate = p.StartDate != null ? FromProtoTs(p.StartDate) : null;
        t.UtcStartDate = p.UtcStartDate != null ? FromProtoTs(p.UtcStartDate) : null;
        t.DueDate = p.DueDate != null ? FromProtoTs(p.DueDate) : null;
        t.UtcDueDate = p.UtcDueDate != null ? FromProtoTs(p.UtcDueDate) : null;
        t.Complete = p.HasComplete ? p.Complete : null;
        t.DateCompleted = p.DateCompleted != null ? FromProtoTs(p.DateCompleted) : null;
        t.ReminderSet = p.HasReminderSet ? p.ReminderSet : null;
        t.ReminderTime = p.ReminderTime != null ? FromProtoTs(p.ReminderTime) : null;
        t.RecurrenceType = p.HasRecurrenceType ? (byte)p.RecurrenceType : null;
        t.RecurrenceStart = p.RecurrenceStart != null ? FromProtoTs(p.RecurrenceStart) : null;
        t.RecurrenceUntil = p.RecurrenceUntil != null ? FromProtoTs(p.RecurrenceUntil) : null;
        t.RecurrenceOccurrences = p.HasRecurrenceOccurrences ? (byte)p.RecurrenceOccurrences : null;
        t.RecurrenceInterval = p.HasRecurrenceInterval ? (ushort)p.RecurrenceInterval : null;
        t.RecurrenceDayOfWeek = p.HasRecurrenceDayOfWeek ? (byte)p.RecurrenceDayOfWeek : null;
        t.RecurrenceDayOfMonth = p.HasRecurrenceDayOfMonth ? (byte)p.RecurrenceDayOfMonth : null;
        t.RecurrenceWeekOfMonth = p.HasRecurrenceWeekOfMonth ? (byte)p.RecurrenceWeekOfMonth : null;
        t.RecurrenceMonthOfYear = p.HasRecurrenceMonthOfYear ? (byte)p.RecurrenceMonthOfYear : null;
        t.RecurrenceRegenerate = p.HasRecurrenceRegenerate ? p.RecurrenceRegenerate : null;
        t.RecurrenceDeadOccur = p.HasRecurrenceDeadOccur ? (byte)p.RecurrenceDeadOccur : null;
        t.RecurrenceCalendarType = p.HasRecurrenceCalendarType ? (byte)p.RecurrenceCalendarType : null;
        t.RecurrenceIsLeapMonth = p.HasRecurrenceIsLeapMonth ? p.RecurrenceIsLeapMonth : null;
        t.RecurrenceFirstDayOfWeek = p.HasRecurrenceFirstDayOfWeek ? (byte)p.RecurrenceFirstDayOfWeek : null;
        return t;
    }

    public static NoteItem ToProto(DbNote n)
    {
        var p = new NoteItem
        {
            LastModifiedDate = ToProtoTs(n.LastModifiedDate),
        };

        if (n.Subject is not null) p.Subject = n.Subject;
        if (n.MessageClass is not null) p.MessageClass = n.MessageClass;
        if (n.Body is not null) p.Body = n.Body;
        if (n.NativeBodyType.HasValue) p.NativeBodyType = n.NativeBodyType.Value;

        p.Categories.AddRange(n.Categories.Select(c => new NoteCategory
        { Id = c.Id, NoteItemId = c.NoteItemId, Category = c.Category }));

        return p;
    }

    public static DbNote ToEntity(string userId, string collectionId, NoteItem p) =>
        ApplyToEntity(new DbNote { UserId = userId, CollectionId = collectionId }, p);

    // Scalar fields only; Categories are synced in MailboxStoreService (as with Contact/Calendar).
    public static DbNote ApplyToEntity(DbNote n, NoteItem p)
    {
        n.Subject = p.HasSubject ? p.Subject : null;
        n.MessageClass = p.HasMessageClass ? p.MessageClass : null;
        n.LastModifiedDate = p.LastModifiedDate != null ? FromProtoTs(p.LastModifiedDate) : null;
        n.Body = p.HasBody ? p.Body : null;
        n.NativeBodyType = p.HasNativeBodyType ? (byte)p.NativeBodyType : null;
        return n;
    }

    public static EmailItem ToProto(DbEmail e)
    {
        var p = new EmailItem
        {
            DateReceived = ToProtoTs(e.DateReceived),
            LastVerbExecutionTime = ToProtoTs(e.LastVerbExecutionTime),
        };

        if (e.To is not null) p.To = e.To;
        if (e.Cc is not null) p.Cc = e.Cc;
        if (e.Bcc is not null) p.Bcc = e.Bcc;
        if (e.From is not null) p.From = e.From;
        if (e.ReplyTo is not null) p.ReplyTo = e.ReplyTo;
        if (e.DisplayTo is not null) p.DisplayTo = e.DisplayTo;
        if (e.Sender is not null) p.Sender = e.Sender;
        if (e.Subject is not null) p.Subject = e.Subject;
        if (e.ThreadTopic is not null) p.ThreadTopic = e.ThreadTopic;
        if (e.Importance.HasValue) p.Importance = e.Importance.Value;
        if (e.Read.HasValue) p.Read = e.Read.Value;
        if (e.MessageClass is not null) p.MessageClass = e.MessageClass;
        if (e.InternetCPID is not null) p.InternetCpid = e.InternetCPID;
        if (e.ContentClass is not null) p.ContentClass = e.ContentClass;
        if (e.ConversationId is { Length: > 0 }) p.ConversationId = Google.Protobuf.ByteString.CopyFrom(e.ConversationId);
        if (e.ConversationIndex is { Length: > 0 }) p.ConversationIndex = Google.Protobuf.ByteString.CopyFrom(e.ConversationIndex);
        if (e.LastVerbExecuted.HasValue) p.LastVerbExecuted = e.LastVerbExecuted.Value;
        if (e.Body is not null) p.Body = e.Body;
        if (e.BodyType.HasValue) p.BodyType = e.BodyType.Value;
        if (e.NativeBodyType.HasValue) p.NativeBodyType = e.NativeBodyType.Value;
        if (e.MimeRaw is not null) p.MimeRaw = e.MimeRaw;

        if (e.FlagStatus.HasValue || e.FlagType is not null || e.FlagSubject is not null)
            p.Flag = ToFlagProto(e);

        p.Attachments.AddRange(e.Attachments.Select(ToProto));

        return p;
    }

    // metadata only - Content is intentionally never populated here, matching the
    // "write-only" contract documented on AttachmentItem.content in the proto
    public static AttachmentItem ToProto(DbAttachment a)
    {
        var p = new AttachmentItem { Id = a.Id };

        if (a.DisplayName is not null) p.DisplayName = a.DisplayName;
        if (a.ContentType is not null) p.ContentType = a.ContentType;
        if (a.EstimatedDataSize.HasValue) p.EstimatedDataSize = a.EstimatedDataSize.Value;
        if (a.Method.HasValue) p.Method = a.Method.Value;
        if (a.ContentId is not null) p.ContentId = a.ContentId;
        if (a.ContentLocation is not null) p.ContentLocation = a.ContentLocation;
        if (a.IsInline.HasValue) p.IsInline = a.IsInline.Value;
        return p;
    }

    public static DbAttachment ToEntity(string emailItemId, AttachmentItem p) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        EmailItemId = emailItemId,
        DisplayName = p.HasDisplayName ? p.DisplayName : null,
        ContentType = p.HasContentType ? p.ContentType : null,
        EstimatedDataSize = p.HasEstimatedDataSize ? p.EstimatedDataSize : null,
        Method = p.HasMethod ? (byte)p.Method : null,
        ContentId = p.HasContentId ? p.ContentId : null,
        ContentLocation = p.HasContentLocation ? p.ContentLocation : null,
        IsInline = p.HasIsInline ? p.IsInline : null,
        Content = p.HasContent ? p.Content.ToByteArray() : null,
    };

    private static EmailFlag ToFlagProto(DbEmail e)
    {
        var f = new EmailFlag
        {
            DateCompleted = ToProtoTs(e.FlagDateCompleted),
            CompleteTime = ToProtoTs(e.FlagCompleteTime),
            StartDate = ToProtoTs(e.FlagStartDate),
            DueDate = ToProtoTs(e.FlagDueDate),
            UtcStartDate = ToProtoTs(e.FlagUtcStartDate),
            UtcDueDate = ToProtoTs(e.FlagUtcDueDate),
            ReminderTime = ToProtoTs(e.FlagReminderTime),
        };

        if (e.FlagStatus.HasValue) f.Status = e.FlagStatus.Value;
        if (e.FlagType is not null) f.FlagType = e.FlagType;
        if (e.FlagSubject is not null) f.Subject = e.FlagSubject;
        if (e.FlagReminderSet.HasValue) f.ReminderSet = e.FlagReminderSet.Value;
        return f;
    }

    public static DbEmail ToEntity(string userId, string collectionId, EmailItem p) =>
        ApplyToEntity(new DbEmail { UserId = userId, CollectionId = collectionId }, p);

    public static DbEmail ApplyToEntity(DbEmail e, EmailItem p)
    {
        e.To = p.HasTo ? p.To : null;
        e.Cc = p.HasCc ? p.Cc : null;
        e.Bcc = p.HasBcc ? p.Bcc : null;
        e.From = p.HasFrom ? p.From : null;
        e.ReplyTo = p.HasReplyTo ? p.ReplyTo : null;
        e.DisplayTo = p.HasDisplayTo ? p.DisplayTo : null;
        e.Sender = p.HasSender ? p.Sender : null;
        e.Subject = p.HasSubject ? p.Subject : null;
        e.DateReceived = p.DateReceived != null ? FromProtoTs(p.DateReceived) : null;
        e.ThreadTopic = p.HasThreadTopic ? p.ThreadTopic : null;
        e.Importance = p.HasImportance ? (byte)p.Importance : null;
        e.Read = p.HasRead ? p.Read : null;
        e.MessageClass = p.HasMessageClass ? p.MessageClass : null;
        e.InternetCPID = p.HasInternetCpid ? p.InternetCpid : null;
        e.ContentClass = p.HasContentClass ? p.ContentClass : null;
        e.ConversationId = p.HasConversationId ? p.ConversationId.ToByteArray() : null;
        e.ConversationIndex = p.HasConversationIndex ? p.ConversationIndex.ToByteArray() : null;
        e.LastVerbExecuted = p.HasLastVerbExecuted ? p.LastVerbExecuted : null;
        e.LastVerbExecutionTime = p.LastVerbExecutionTime != null ? FromProtoTs(p.LastVerbExecutionTime) : null;
        e.Body = p.HasBody ? p.Body : null;
        e.BodyType = p.HasBodyType ? (byte)p.BodyType : null;
        e.NativeBodyType = p.HasNativeBodyType ? (byte)p.NativeBodyType : null;
        e.MimeRaw = p.HasMimeRaw ? p.MimeRaw : null;

        if (p.Flag is { } f)
        {
            e.FlagStatus = f.HasStatus ? (byte)f.Status : null;
            e.FlagType = f.HasFlagType ? f.FlagType : null;
            e.FlagSubject = f.HasSubject ? f.Subject : null;
            e.FlagDateCompleted = f.DateCompleted != null ? FromProtoTs(f.DateCompleted) : null;
            e.FlagCompleteTime = f.CompleteTime != null ? FromProtoTs(f.CompleteTime) : null;
            e.FlagStartDate = f.StartDate != null ? FromProtoTs(f.StartDate) : null;
            e.FlagDueDate = f.DueDate != null ? FromProtoTs(f.DueDate) : null;
            e.FlagUtcStartDate = f.UtcStartDate != null ? FromProtoTs(f.UtcStartDate) : null;
            e.FlagUtcDueDate = f.UtcDueDate != null ? FromProtoTs(f.UtcDueDate) : null;
            e.FlagReminderSet = f.HasReminderSet ? f.ReminderSet : null;
            e.FlagReminderTime = f.ReminderTime != null ? FromProtoTs(f.ReminderTime) : null;
        }
        else
        {
            e.FlagStatus = null;
            e.FlagType = null;
            e.FlagSubject = null;
            e.FlagDateCompleted = e.FlagCompleteTime = e.FlagStartDate = e.FlagDueDate = null;
            e.FlagUtcStartDate = e.FlagUtcDueDate = e.FlagReminderTime = null;
            e.FlagReminderSet = null;
        }

        return e;
    }
}
