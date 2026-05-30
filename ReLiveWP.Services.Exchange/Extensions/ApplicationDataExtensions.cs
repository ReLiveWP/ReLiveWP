using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Helpers;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Extensions;

internal static class ApplicationDataExtensions
{ 
    public static ApplicationData ToApplicationData(this ContactItem c)
    {
        var cd = new ContactData();

        cd.FirstName = c.HasFirstName ? c.FirstName : null;
        cd.MiddleName = c.HasMiddleName ? c.MiddleName : null;
        cd.LastName = c.HasLastName ? c.LastName : null;
        cd.Title = c.HasTitle ? c.Title : null;
        cd.Suffix = c.HasSuffix ? c.Suffix : null;
        cd.FileAs = c.HasFileAs ? c.FileAs : null;
        cd.Alias = c.HasAlias ? c.Alias : null;
        cd.NickName = c.HasNickName ? c.NickName : null;
        cd.YomiFirstName = c.HasYomiFirstName ? c.YomiFirstName : null;
        cd.YomiLastName = c.HasYomiLastName ? c.YomiLastName : null;
        cd.YomiCompanyName = c.HasYomiCompanyName ? c.YomiCompanyName : null;
        cd.CompanyName = c.HasCompanyName ? c.CompanyName : null;
        cd.Department = c.HasDepartment ? c.Department : null;
        cd.JobTitle = c.HasJobTitle ? c.JobTitle : null;
        cd.OfficeLocation = c.HasOfficeLocation ? c.OfficeLocation : null;
        cd.AccountName = c.HasAccountName ? c.AccountName : null;
        cd.ManagerName = c.HasManagerName ? c.ManagerName : null;
        cd.CustomerId = c.HasCustomerId ? c.CustomerId : null;
        cd.GovernmentId = c.HasGovernmentId ? c.GovernmentId : null;
        cd.AssistantName = c.HasAssistantName ? c.AssistantName : null;
        cd.Email1Address = c.HasEmail1Address ? c.Email1Address : null;
        cd.Email2Address = c.HasEmail2Address ? c.Email2Address : null;
        cd.Email3Address = c.HasEmail3Address ? c.Email3Address : null;
        cd.BusinessPhoneNumber = c.HasBusinessPhoneNumber ? c.BusinessPhoneNumber : null;
        cd.Business2PhoneNumber = c.HasBusiness2PhoneNumber ? c.Business2PhoneNumber : null;
        cd.BusinessFaxNumber = c.HasBusinessFaxNumber ? c.BusinessFaxNumber : null;
        cd.HomePhoneNumber = c.HasHomePhoneNumber ? c.HomePhoneNumber : null;
        cd.Home2PhoneNumber = c.HasHome2PhoneNumber ? c.Home2PhoneNumber : null;
        cd.HomeFaxNumber = c.HasHomeFaxNumber ? c.HomeFaxNumber : null;
        cd.MobilePhoneNumber = c.HasMobilePhoneNumber ? c.MobilePhoneNumber : null;
        cd.CarPhoneNumber = c.HasCarPhoneNumber ? c.CarPhoneNumber : null;
        cd.PagerNumber = c.HasPagerNumber ? c.PagerNumber : null;
        cd.RadioPhoneNumber = c.HasRadioPhoneNumber ? c.RadioPhoneNumber : null;
        cd.AssistantPhoneNumber = c.HasAssistantPhoneNumber ? c.AssistantPhoneNumber : null;
        cd.CompanyMainPhone = c.HasCompanyMainPhone ? c.CompanyMainPhone : null;
        cd.MMS = c.HasMms ? c.Mms : null;
        cd.IMAddress = c.HasImAddress ? c.ImAddress : null;
        cd.IMAddress2 = c.HasImAddress2 ? c.ImAddress2 : null;
        cd.IMAddress3 = c.HasImAddress3 ? c.ImAddress3 : null;
        cd.BusinessAddressStreet = c.HasBusinessAddressStreet ? c.BusinessAddressStreet : null;
        cd.BusinessAddressCity = c.HasBusinessAddressCity ? c.BusinessAddressCity : null;
        cd.BusinessAddressState = c.HasBusinessAddressState ? c.BusinessAddressState : null;
        cd.BusinessAddressPostalCode = c.HasBusinessAddressPostalCode ? c.BusinessAddressPostalCode : null;
        cd.BusinessAddressCountry = c.HasBusinessAddressCountry ? c.BusinessAddressCountry : null;
        cd.HomeAddressStreet = c.HasHomeAddressStreet ? c.HomeAddressStreet : null;
        cd.HomeAddressCity = c.HasHomeAddressCity ? c.HomeAddressCity : null;
        cd.HomeAddressState = c.HasHomeAddressState ? c.HomeAddressState : null;
        cd.HomeAddressPostalCode = c.HasHomeAddressPostalCode ? c.HomeAddressPostalCode : null;
        cd.HomeAddressCountry = c.HasHomeAddressCountry ? c.HomeAddressCountry : null;
        cd.OtherAddressStreet = c.HasOtherAddressStreet ? c.OtherAddressStreet : null;
        cd.OtherAddressCity = c.HasOtherAddressCity ? c.OtherAddressCity : null;
        cd.OtherAddressState = c.HasOtherAddressState ? c.OtherAddressState : null;
        cd.OtherAddressPostalCode = c.HasOtherAddressPostalCode ? c.OtherAddressPostalCode : null;
        cd.OtherAddressCountry = c.HasOtherAddressCountry ? c.OtherAddressCountry : null;
        cd.Spouse = c.HasSpouse ? c.Spouse : null;
        cd.WebPage = c.HasWebPage ? c.WebPage : null;
        cd.Picture = c.HasPicture ? c.Picture.ToByteArray() : null;
        cd.Birthday = c.Birthday != null ? c.Birthday.ToDateTime() : null;
        cd.Anniversary = c.Anniversary != null ? c.Anniversary.ToDateTime() : null;

        if (c.HasNotes)
            cd.Body = new AirSyncBody { Type = BodyType.PlainText, Data = c.Notes };

        return DtoToApplicationData(cd);
    }

    public static ApplicationData ToApplicationData(this CalendarItem cal)
    {
        var cd = new CalendarData();
        cd.Timezone = cal.HasTimezone ? cal.Timezone : null;
        cd.StartTime = cal.StartTime != null ? cal.StartTime.ToDateTime() : null;
        cd.EndTime = cal.EndTime != null ? cal.EndTime.ToDateTime() : null;
        cd.DtStamp = cal.DtStamp != null ? cal.DtStamp.ToDateTime() : null;
        cd.Uid = cal.HasUid ? cal.Uid : null;
        cd.Subject = cal.HasSubject ? cal.Subject : null;
        cd.Location = cal.HasLocation ? cal.Location : null;
        cd.OrganizerName = cal.HasOrganizerName ? cal.OrganizerName : null;
        cd.OrganizerEmail = cal.HasOrganizerEmail ? cal.OrganizerEmail : null;
        if (cal.HasReminder)
            cd.Reminder = cal.Reminder;

        if (cal.HasAllDayEvent)
            cd.AllDayEvent = cal.AllDayEvent ? (byte)1 : (byte)0;

        if (cal.HasBusyStatus)
            cd.BusyStatus = (byte)cal.BusyStatus;

        if (cal.HasSensitivity)
            cd.Sensitivity = (byte)cal.Sensitivity;

        if (cal.HasMeetingStatus)
            cd.MeetingStatus = (byte)cal.MeetingStatus;

        if (cal.HasResponseType)
            cd.ResponseType = cal.ResponseType;

        if (cal.HasResponseRequested)
            cd.ResponseRequested = cal.ResponseRequested ? 1 : 0;

        if (cal.HasDisallowNewTimeProposal)
            cd.DisallowNewTimeProposal = cal.DisallowNewTimeProposal ? 1 : 0;

        if (cal.HasNotes)
            cd.Body = new AirSyncBody { Type = BodyType.PlainText, Data = cal.Notes };

        if (cal.HasBodyTruncated)
            cd.BodyTruncated = cal.BodyTruncated ? 1 : 0;

        if (cal.HasRecurrenceType)
        {
            cd.Recurrence = new CalendarRecurrence
            {
                Type = cal.HasRecurrenceType ? (byte?)cal.RecurrenceType : null,
                Occurrences = cal.HasRecurrenceOccurrences ? (ushort?)cal.RecurrenceOccurrences : null,
                Interval = cal.HasRecurrenceInterval ? (ushort?)cal.RecurrenceInterval : null,
                WeekOfMonth = cal.HasRecurrenceWeekOfMonth ? (byte?)cal.RecurrenceWeekOfMonth : null,
                DayOfWeek = cal.HasRecurrenceDayOfWeek ? (ushort?)cal.RecurrenceDayOfWeek : null,
                MonthOfYear = cal.HasRecurrenceMonthOfYear ? (byte?)cal.RecurrenceMonthOfYear : null,
                DayOfMonth = cal.HasRecurrenceDayOfMonth ? (byte?)cal.RecurrenceDayOfMonth : null,
                CalendarType = cal.HasRecurrenceCalendarType ? (byte?)cal.RecurrenceCalendarType : null,
                IsLeapMonth = cal.HasRecurrenceIsLeapMonth ? (cal.RecurrenceIsLeapMonth ? (byte)1 : (byte)0) : null,
                FirstDayOfWeek = cal.HasRecurrenceFirstDayOfWeek ? (byte?)cal.RecurrenceFirstDayOfWeek : null,
                Until = cal.RecurrenceUntil != null ? EasDateHelper.FromDateTime(cal.RecurrenceUntil.ToDateTime()) : null,
            };
        }
        return DtoToApplicationData(cd);
    }

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
}
