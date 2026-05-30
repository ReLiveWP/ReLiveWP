using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Exchange.Helpers;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Extensions;

// Extension methods that convert EAS XML DTOs → typed proto messages for
// use with the MailboxStore gRPC client (Create/Update item calls).
public static class ProtoExtensions
{
    private static Timestamp? Ts(DateTime? dt) =>
        dt.HasValue ? Timestamp.FromDateTime(DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)) : null;

    public static ContactItem ToProtoContact(this ContactData cd)
    {
        var p = new ContactItem();
        if (cd.FirstName is not null) p.FirstName = cd.FirstName;
        if (cd.MiddleName is not null) p.MiddleName = cd.MiddleName;
        if (cd.LastName is not null) p.LastName = cd.LastName;
        if (cd.Title is not null) p.Title = cd.Title;
        if (cd.Suffix is not null) p.Suffix = cd.Suffix;
        if (cd.FileAs is not null) p.FileAs = cd.FileAs;
        if (cd.Alias is not null) p.Alias = cd.Alias;
        if (cd.NickName is not null) p.NickName = cd.NickName;
        if (cd.YomiFirstName is not null) p.YomiFirstName = cd.YomiFirstName;
        if (cd.YomiLastName is not null) p.YomiLastName = cd.YomiLastName;
        if (cd.YomiCompanyName is not null) p.YomiCompanyName = cd.YomiCompanyName;
        if (cd.CompanyName is not null) p.CompanyName = cd.CompanyName;
        if (cd.Department is not null) p.Department = cd.Department;
        if (cd.JobTitle is not null) p.JobTitle = cd.JobTitle;
        if (cd.OfficeLocation is not null) p.OfficeLocation = cd.OfficeLocation;
        if (cd.AccountName is not null) p.AccountName = cd.AccountName;
        if (cd.ManagerName is not null) p.ManagerName = cd.ManagerName;
        if (cd.CustomerId is not null) p.CustomerId = cd.CustomerId;
        if (cd.GovernmentId is not null) p.GovernmentId = cd.GovernmentId;
        if (cd.AssistantName is not null) p.AssistantName = cd.AssistantName;
        if (cd.Email1Address is not null) p.Email1Address = cd.Email1Address;
        if (cd.Email2Address is not null) p.Email2Address = cd.Email2Address;
        if (cd.Email3Address is not null) p.Email3Address = cd.Email3Address;
        if (cd.BusinessPhoneNumber is not null) p.BusinessPhoneNumber = cd.BusinessPhoneNumber;
        if (cd.Business2PhoneNumber is not null) p.Business2PhoneNumber = cd.Business2PhoneNumber;
        if (cd.BusinessFaxNumber is not null) p.BusinessFaxNumber = cd.BusinessFaxNumber;
        if (cd.HomePhoneNumber is not null) p.HomePhoneNumber = cd.HomePhoneNumber;
        if (cd.Home2PhoneNumber is not null) p.Home2PhoneNumber = cd.Home2PhoneNumber;
        if (cd.HomeFaxNumber is not null) p.HomeFaxNumber = cd.HomeFaxNumber;
        if (cd.MobilePhoneNumber is not null) p.MobilePhoneNumber = cd.MobilePhoneNumber;
        if (cd.CarPhoneNumber is not null) p.CarPhoneNumber = cd.CarPhoneNumber;
        if (cd.PagerNumber is not null) p.PagerNumber = cd.PagerNumber;
        if (cd.RadioPhoneNumber is not null) p.RadioPhoneNumber = cd.RadioPhoneNumber;
        if (cd.AssistantPhoneNumber is not null) p.AssistantPhoneNumber = cd.AssistantPhoneNumber;
        if (cd.CompanyMainPhone is not null) p.CompanyMainPhone = cd.CompanyMainPhone;
        if (cd.MMS is not null) p.Mms = cd.MMS;
        if (cd.IMAddress is not null) p.ImAddress = cd.IMAddress;
        if (cd.IMAddress2 is not null) p.ImAddress2 = cd.IMAddress2;
        if (cd.IMAddress3 is not null) p.ImAddress3 = cd.IMAddress3;
        if (cd.BusinessAddressStreet is not null) p.BusinessAddressStreet = cd.BusinessAddressStreet;
        if (cd.BusinessAddressCity is not null) p.BusinessAddressCity = cd.BusinessAddressCity;
        if (cd.BusinessAddressState is not null) p.BusinessAddressState = cd.BusinessAddressState;
        if (cd.BusinessAddressPostalCode is not null) p.BusinessAddressPostalCode = cd.BusinessAddressPostalCode;
        if (cd.BusinessAddressCountry is not null) p.BusinessAddressCountry = cd.BusinessAddressCountry;
        if (cd.HomeAddressStreet is not null) p.HomeAddressStreet = cd.HomeAddressStreet;
        if (cd.HomeAddressCity is not null) p.HomeAddressCity = cd.HomeAddressCity;
        if (cd.HomeAddressState is not null) p.HomeAddressState = cd.HomeAddressState;
        if (cd.HomeAddressPostalCode is not null) p.HomeAddressPostalCode = cd.HomeAddressPostalCode;
        if (cd.HomeAddressCountry is not null) p.HomeAddressCountry = cd.HomeAddressCountry;
        if (cd.OtherAddressStreet is not null) p.OtherAddressStreet = cd.OtherAddressStreet;
        if (cd.OtherAddressCity is not null) p.OtherAddressCity = cd.OtherAddressCity;
        if (cd.OtherAddressState is not null) p.OtherAddressState = cd.OtherAddressState;
        if (cd.OtherAddressPostalCode is not null) p.OtherAddressPostalCode = cd.OtherAddressPostalCode;
        if (cd.OtherAddressCountry is not null) p.OtherAddressCountry = cd.OtherAddressCountry;
        if (cd.Spouse is not null) p.Spouse = cd.Spouse;
        if (cd.WebPage is not null) p.WebPage = cd.WebPage;
        if (cd.Birthday.HasValue) p.Birthday = Ts(cd.Birthday);
        if (cd.Anniversary.HasValue) p.Anniversary = Ts(cd.Anniversary);
        if (cd.Picture is { Length: > 0 }) p.Picture = Google.Protobuf.ByteString.CopyFrom(cd.Picture);
        var notes = cd.Body?.Data ?? cd.BodyLegacy;
        if (notes is not null) p.Notes = notes;

        p.Categories.AddRange(cd.Categories?.Items.Select(n => new ContactCategory
        { Id = Guid.NewGuid().ToString("N"), Name = n }) ?? []);
        p.Children.AddRange(cd.Children?.Items.Select(n => new ContactChild
        { Id = Guid.NewGuid().ToString("N"), Name = n }) ?? []);

        return p;
    }

    public static CalendarItem ToProtoCalendar(this CalendarData cd)
    {
        var p = new CalendarItem
        {
            StartTime = Ts(cd.StartTime),
            EndTime = Ts(cd.EndTime),
            DtStamp = Ts(cd.DtStamp),
            AppointmentReplyTime = Ts(cd.AppointmentReplyTime),
        };

        if (cd.Timezone is not null) p.Timezone = cd.Timezone;
        if (cd.Uid is not null) p.Uid = cd.Uid;
        if (cd.ClientUid is not null) p.ClientUid = cd.ClientUid;
        if (cd.Subject is not null) p.Subject = cd.Subject;
        if (cd.Location is not null) p.Location = cd.Location;
        if (cd.OrganizerName is not null) p.OrganizerName = cd.OrganizerName;
        if (cd.OrganizerEmail is not null) p.OrganizerEmail = cd.OrganizerEmail;
        if (cd.OnlineMeetingConfLink is not null) p.OnlineMeetingConfLink = cd.OnlineMeetingConfLink;
        if (cd.OnlineMeetingExternalLink is not null) p.OnlineMeetingExternalLink = cd.OnlineMeetingExternalLink;
        if (cd.Reminder.HasValue) p.Reminder = cd.Reminder.Value;
        if (cd.AllDayEvent.HasValue) p.AllDayEvent = cd.AllDayEvent != 0;
        if (cd.BusyStatus.HasValue) p.BusyStatus = cd.BusyStatus.Value;
        if (cd.Sensitivity.HasValue) p.Sensitivity = cd.Sensitivity.Value;
        if (cd.MeetingStatus.HasValue) p.MeetingStatus = cd.MeetingStatus.Value;
        if (cd.NativeBodyType.HasValue) p.NativeBodyType = cd.NativeBodyType.Value;
        if (cd.ResponseType.HasValue) p.ResponseType = cd.ResponseType.Value;
        if (cd.ResponseRequested.HasValue) p.ResponseRequested = cd.ResponseRequested != 0;
        if (cd.DisallowNewTimeProposal.HasValue) p.DisallowNewTimeProposal = cd.DisallowNewTimeProposal != 0;
        if (cd.BodyTruncated.HasValue) p.BodyTruncated = cd.BodyTruncated != 0;
        var notes = cd.Body?.Data ?? cd.BodyLegacy;
        if (notes is not null) p.Notes = notes;

        if (cd.Recurrence is { } r)
        {
            if (r.Type.HasValue) p.RecurrenceType = r.Type.Value;
            if (r.Occurrences.HasValue) p.RecurrenceOccurrences = r.Occurrences.Value;
            if (r.Interval.HasValue) p.RecurrenceInterval = r.Interval.Value;
            if (r.WeekOfMonth.HasValue) p.RecurrenceWeekOfMonth = r.WeekOfMonth.Value;
            if (r.DayOfWeek.HasValue) p.RecurrenceDayOfWeek = r.DayOfWeek.Value;
            if (r.MonthOfYear.HasValue) p.RecurrenceMonthOfYear = r.MonthOfYear.Value;
            if (r.DayOfMonth.HasValue) p.RecurrenceDayOfMonth = r.DayOfMonth.Value;
            if (r.CalendarType.HasValue) p.RecurrenceCalendarType = r.CalendarType.Value;
            if (r.IsLeapMonth.HasValue) p.RecurrenceIsLeapMonth = r.IsLeapMonth != 0;
            if (r.FirstDayOfWeek.HasValue) p.RecurrenceFirstDayOfWeek = r.FirstDayOfWeek.Value;
            p.RecurrenceUntil = Ts(EasDateHelper.ToDateTime(r.Until));
        }

        p.Attendees.AddRange(cd.Attendees?.Items.Select(a => new CalendarAttendee
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = a.Email,
            Name = a.Name,
        }) ?? []);
        p.Categories.AddRange(cd.Categories?.Items.Select(c => new CalendarCategory { Id = Guid.NewGuid().ToString("N"), Category = c }) ?? []);
        p.Exceptions.AddRange(cd.Exceptions?.Items.Select(ex =>
        {
            var exId = Guid.NewGuid().ToString("N");
            var dbEx = new CalendarException
            {
                Id = exId,
                ExceptionStartTime = ex.ExceptionStartTime,
                InstanceId = ex.InstanceId,
                Subject = ex.Subject,
                StartTime = Ts(ex.StartTime),
                EndTime = Ts(ex.EndTime),
                Location = ex.Location,
                DtStamp = Ts(ex.DtStamp),
                AppointmentReplyTime = Ts(ex.AppointmentReplyTime),
            };
            if (ex.Deleted.HasValue) dbEx.Deleted = ex.Deleted != 0;
            if (ex.Sensitivity.HasValue) dbEx.Sensitivity = ex.Sensitivity.Value;
            if (ex.BusyStatus.HasValue) dbEx.BusyStatus = ex.BusyStatus.Value;
            if (ex.AllDayEvent.HasValue) dbEx.AllDayEvent = ex.AllDayEvent != 0;
            if (ex.Reminder.HasValue) dbEx.Reminder = ex.Reminder.Value;
            if (ex.MeetingStatus.HasValue) dbEx.MeetingStatus = ex.MeetingStatus.Value;
            if (ex.ResponseType.HasValue) dbEx.ResponseType = ex.ResponseType.Value;
            if (ex.OnlineMeetingConfLink is not null) dbEx.OnlineMeetingConfLink = ex.OnlineMeetingConfLink;
            if (ex.OnlineMeetingExternalLink is not null) dbEx.OnlineMeetingExternalLink = ex.OnlineMeetingExternalLink;
            var exNotes = ex.Body?.Data ?? ex.BodyLegacy;
            if (exNotes is not null) dbEx.Notes = exNotes;
            if (ex.BodyLegacy is not null) dbEx.BodyLegacy = ex.BodyLegacy;
            dbEx.Attendees.AddRange(ex.Attendees?.Items.Select(a => new CalendarExceptionAttendee
            {
                Id = Guid.NewGuid().ToString("N"),
                CalendarExceptionId = exId,
                Email = a.Email,
                Name = a.Name,
            }) ?? []);
            dbEx.Categories.AddRange(ex.Categories?.Items.Select(c => new CalendarExceptionCategory
            { Id = Guid.NewGuid().ToString("N"), CalendarExceptionId = exId, Category = c }) ?? []);
            return dbEx;
        }) ?? []);

        return p;
    }
}
