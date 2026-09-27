using System.Text;
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

        if (c.Categories.Count > 0)
            cd.Categories = new ContactCategories { Items = [.. c.Categories.Select(x => x.Name)] };

        if (c.Children.Count > 0)
            cd.Children = new ContactChildren { Items = [.. c.Children.Select(x => x.Name)] };

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

        // calendar:BodyTruncated is 2.5-only (MS-ASCAL 2.2.2.8) and has no token on code page 4,
        // so emitting it produced an undecodable stream. 12.0+ reports truncation on
        // airsyncbase:Body/Truncated instead.

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
                Until = cal.RecurrenceUntil != null ? EasDateHelper.FromDateTimeCompact(cal.RecurrenceUntil.ToDateTime()) : null,
            };
        }

        if (cal.Attendees.Count > 0)
            cd.Attendees = new CalendarAttendees { Items = [.. cal.Attendees.Select(ToAttendeeData)] };

        if (cal.Categories.Count > 0)
            cd.Categories = new CalendarCategories { Items = [.. cal.Categories.Select(x => x.Category)] };

        if (cal.Exceptions.Count > 0)
            cd.Exceptions = new CalendarExceptions { Items = [.. cal.Exceptions.Select(ToExceptionData)] };

        return DtoToApplicationData(cd);
    }

    private static CalendarAttendeeData ToAttendeeData(CalendarAttendee a) => new()
    {
        Email = a.Email,
        Name = a.Name,
        AttendeeStatus = a.HasAttendeeStatus ? (byte?)a.AttendeeStatus : null,
        AttendeeType = a.HasAttendeeType ? (byte?)a.AttendeeType : null,
    };

    private static CalendarExceptionData ToExceptionData(CalendarException ex)
    {
        var data = new CalendarExceptionData
        {
            // required on every Exception (MS-ASCAL 2.2.2.23); already stored in compact form
            ExceptionStartTime = ex.HasExceptionStartTime ? ex.ExceptionStartTime : null,
            Subject = ex.HasSubject ? ex.Subject : null,
            StartTime = ex.StartTime?.ToDateTime(),
            EndTime = ex.EndTime?.ToDateTime(),
            Location = ex.HasLocation ? ex.Location : null,
            DtStamp = ex.DtStamp?.ToDateTime(),
            AppointmentReplyTime = ex.AppointmentReplyTime?.ToDateTime(),
        };

        if (ex.HasDeleted) data.Deleted = ex.Deleted ? (byte)1 : (byte)0;
        if (ex.HasSensitivity) data.Sensitivity = (byte)ex.Sensitivity;
        if (ex.HasBusyStatus) data.BusyStatus = (byte)ex.BusyStatus;
        if (ex.HasAllDayEvent) data.AllDayEvent = ex.AllDayEvent ? (byte)1 : (byte)0;
        if (ex.HasReminder) data.Reminder = ex.Reminder;
        if (ex.HasMeetingStatus) data.MeetingStatus = (byte)ex.MeetingStatus;
        if (ex.HasResponseType) data.ResponseType = ex.ResponseType;
        if (ex.HasOnlineMeetingConfLink) data.OnlineMeetingConfLink = ex.OnlineMeetingConfLink;
        if (ex.HasOnlineMeetingExternalLink) data.OnlineMeetingExternalLink = ex.OnlineMeetingExternalLink;
        if (ex.HasNotes) data.Body = new AirSyncBody { Type = BodyType.PlainText, Data = ex.Notes };

        if (ex.Attendees.Count > 0)
            data.Attendees = new CalendarAttendees
            {
                Items = [.. ex.Attendees.Select(a => new CalendarAttendeeData
                {
                    Email = a.Email,
                    Name = a.Name,
                    AttendeeStatus = a.HasAttendeeStatus ? (byte?)a.AttendeeStatus : null,
                    AttendeeType = a.HasAttendeeType ? (byte?)a.AttendeeType : null,
                })],
            };

        if (ex.Categories.Count > 0)
            data.Categories = new CalendarCategories { Items = [.. ex.Categories.Select(c => c.Category)] };

        return data;
    }

    public static ApplicationData ToApplicationData(this TaskItem t)
    {
        var td = new TaskData
        {
            Subject = t.HasSubject ? t.Subject : null,
            StartDate = t.StartDate != null ? t.StartDate.ToDateTime() : null,
            UtcStartDate = t.UtcStartDate != null ? t.UtcStartDate.ToDateTime() : null,
            DueDate = t.DueDate != null ? t.DueDate.ToDateTime() : null,
            UtcDueDate = t.UtcDueDate != null ? t.UtcDueDate.ToDateTime() : null,
            DateCompleted = t.DateCompleted != null ? t.DateCompleted.ToDateTime() : null,
            ReminderTime = t.ReminderTime != null ? t.ReminderTime.ToDateTime() : null,
        };

        if (t.HasImportance) td.Importance = (byte)t.Importance;
        if (t.HasSensitivity) td.Sensitivity = (byte)t.Sensitivity;
        if (t.HasComplete) td.Complete = t.Complete ? (byte)1 : (byte)0;
        // WP7 refuses a completed task with no DateCompleted (silently fails to render);
        // backstop for rows written before completion-time stamping existed
        if (td.Complete == 1 && td.DateCompleted is null)
            td.DateCompleted = DateTime.UtcNow;
        if (t.HasReminderSet) td.ReminderSet = t.ReminderSet ? (byte)1 : (byte)0;
        if (t.HasNotes) td.Body = new AirSyncBody { Type = BodyType.PlainText, Data = t.Notes };
        if (t.HasNativeBodyType) td.NativeBodyType = (byte)t.NativeBodyType;

        if (t.HasRecurrenceType)
        {
            td.Recurrence = new TaskRecurrence
            {
                Type = (byte?)t.RecurrenceType,
                Start = t.RecurrenceStart != null ? t.RecurrenceStart.ToDateTime() : null,
                Until = t.RecurrenceUntil != null ? t.RecurrenceUntil.ToDateTime() : null,
                Occurrences = t.HasRecurrenceOccurrences ? (byte?)t.RecurrenceOccurrences : null,
                Interval = t.HasRecurrenceInterval ? (ushort?)t.RecurrenceInterval : null,
                DayOfWeek = t.HasRecurrenceDayOfWeek ? (byte?)t.RecurrenceDayOfWeek : null,
                DayOfMonth = t.HasRecurrenceDayOfMonth ? (byte?)t.RecurrenceDayOfMonth : null,
                WeekOfMonth = t.HasRecurrenceWeekOfMonth ? (byte?)t.RecurrenceWeekOfMonth : null,
                MonthOfYear = t.HasRecurrenceMonthOfYear ? (byte?)t.RecurrenceMonthOfYear : null,
                Regenerate = t.HasRecurrenceRegenerate ? (t.RecurrenceRegenerate ? (byte)1 : (byte)0) : null,
                DeadOccur = t.HasRecurrenceDeadOccur ? (byte?)t.RecurrenceDeadOccur : null,
                CalendarType = t.HasRecurrenceCalendarType ? (byte?)t.RecurrenceCalendarType : null,
                IsLeapMonth = t.HasRecurrenceIsLeapMonth ? (t.RecurrenceIsLeapMonth ? (byte)1 : (byte)0) : null,
                FirstDayOfWeek = t.HasRecurrenceFirstDayOfWeek ? (byte?)t.RecurrenceFirstDayOfWeek : null,
            };
        }
        return DtoToApplicationData(td);
    }

    public static ApplicationData ToApplicationData(this NoteItem n)
    {
        var nd = new NoteData
        {
            Subject = n.HasSubject ? n.Subject : null,
            MessageClass = n.HasMessageClass ? n.MessageClass : "IPM.StickyNote",
            // required in a Sync response; backstop for rows written before it was stamped
            LastModifiedDate = n.LastModifiedDate != null ? n.LastModifiedDate.ToDateTime() : DateTime.UtcNow,
        };

        if (n.HasBody)
            nd.Body = new AirSyncBody { Type = BodyType.PlainText, Data = n.Body };
        if (n.HasNativeBodyType)
            nd.NativeBodyType = (byte)n.NativeBodyType;

        if (n.Categories.Count > 0)
            nd.Categories = new NoteCategories { Items = [.. n.Categories.Select(c => c.Category)] };

        return DtoToApplicationData(nd);
    }

    public static ApplicationData ToApplicationData(this EmailItem e, IReadOnlyList<BodyPreference>? bodyPrefs = null)
    {
        var ed = new EmailData();
        ed.To = e.HasTo ? e.To : null;
        ed.Cc = e.HasCc ? e.Cc : null;
        ed.From = e.HasFrom ? e.From : null;
        ed.ReplyTo = e.HasReplyTo ? e.ReplyTo : null;
        ed.Sender = e.HasSender ? e.Sender : null;
        ed.DisplayTo = e.HasDisplayTo ? e.DisplayTo : null;
        ed.Subject = e.HasSubject ? e.Subject : null;
        ed.DateReceived = e.DateReceived != null ? e.DateReceived.ToDateTime() : null;
        ed.ThreadTopic = e.HasThreadTopic ? e.ThreadTopic : null;
        ed.Importance = e.HasImportance ? (byte)e.Importance : null;
        ed.Read = e.HasRead ? (e.Read ? (byte)1 : (byte)0) : null;
        ed.MessageClass = e.HasMessageClass ? e.MessageClass : "IPM.Note";
        ed.InternetCPID = e.HasInternetCpid ? e.InternetCpid : null;
        ed.ContentClass = e.HasContentClass ? e.ContentClass : null;
        ed.LastVerbExecuted = e.HasLastVerbExecuted ? e.LastVerbExecuted : null;
        ed.LastVerbExecutionTime = e.LastVerbExecutionTime != null ? e.LastVerbExecutionTime.ToDateTime() : null;

        // required in server responses; derive a stable id from the thread when not stored so
        // messages in the same thread group together
        ed.ConversationId = e.HasConversationId ? e.ConversationId.ToByteArray() : DeriveConversationId(e);
        ed.ConversationIndex = e.HasConversationIndex
            ? e.ConversationIndex.ToByteArray()
            : BuildConversationIndex(ed.ConversationId, e.DateReceived?.ToDateTime() ?? DateTime.UtcNow);

        if (e.HasNativeBodyType)
            ed.NativeBodyType = (byte)e.NativeBodyType;

        ed.Body = BuildBody(e, bodyPrefs);

        if (e.Flag is { } f)
            ed.Flag = ToFlagDto(f);

        if (e.Attachments.Count > 0)
            ed.Attachments = new AirSyncAttachments { Items = [.. e.Attachments.Select(ToAttachmentDto)] };

        return DtoToApplicationData(ed);
    }

    private static AirSyncAttachment ToAttachmentDto(AttachmentItem a) => new()
    {
        DisplayName = a.HasDisplayName ? a.DisplayName : null,
        FileReference = a.Id,
        Method = a.HasMethod ? (byte)a.Method : (byte)1,
        EstimatedDataSize = a.HasEstimatedDataSize ? a.EstimatedDataSize : 0,
        ContentId = a.HasContentId ? a.ContentId : null,
        ContentLocation = a.HasContentLocation ? a.ContentLocation : null,
        IsInline = a.HasIsInline && a.IsInline,
    };

    // The native storage format of the stored body. NativeBodyType is authoritative when set,
    // otherwise fall back to the type the body was written as.
    private static BodyType NativeType(EmailItem e) =>
        e.HasNativeBodyType ? (BodyType)e.NativeBodyType
        : e.HasBodyType ? (BodyType)e.BodyType
        : BodyType.PlainText;

    // MS-ASAIRS 2.2.2.9: "the server returns the data ... for the Type element that matches the
    // native storage format". If the client didn't offer the native type it still gets an answer,
    // converted to the first type it did offer, and the response Type says which one that is.
    private static BodyPreference? SelectPreference(IReadOnlyList<BodyPreference> prefs, EmailItem e)
    {
        var native = NativeType(e);

        // MIME can always be served when it's stored, regardless of the native body format
        var mime = prefs.FirstOrDefault(p => p.Type == BodyType.MIME);
        if (mime is not null && e.HasMimeRaw) return mime;

        var exact = prefs.FirstOrDefault(p => p.Type == native);
        if (exact is not null && !SuppressedByAllOrNone(exact, e, native)) return exact;

        // AllOrNone on the native type means "don't send me a partial one of these" - fall through
        // to whichever remaining preference yields the most text (MS-ASAIRS 2.2.2.3.2)
        return prefs.Where(p => p != exact && p.Type != BodyType.MIME)
                    .OrderByDescending(p => p.TruncationSize ?? int.MaxValue)
                    .FirstOrDefault()
               ?? exact
               ?? prefs.FirstOrDefault();
    }

    private static bool SuppressedByAllOrNone(BodyPreference pref, EmailItem e, BodyType native) =>
        pref.AllOrNone is 1
        && pref.TruncationSize is { } size
        && BodyConverter.ByteLength(Convert(e, native, pref.Type)) > size;

    private static string Convert(EmailItem e, BodyType native, BodyType target)
    {
        // stored as octets; Latin1 maps them back one-for-one for the WBXML string element
        if (target == BodyType.MIME) return e.HasMimeRaw ? Encoding.Latin1.GetString(e.MimeRaw.Span) : string.Empty;

        var data = e.HasBody ? e.Body : string.Empty;
        if (target == native) return data;

        return (native, target) switch
        {
            (BodyType.HTML, BodyType.PlainText) => BodyConverter.HtmlToPlainText(data),
            (BodyType.PlainText, BodyType.HTML) => BodyConverter.PlainTextToHtml(data),
            // RTF in either direction isn't supported; hand back what we have rather than nothing
            _ => data,
        };
    }

    // selects the body type the client asked for, converting when the item isn't stored that way,
    // truncates to TruncationSize (a byte count), and reports EstimatedDataSize/Truncated
    private static AirSyncBody? BuildBody(EmailItem e, IReadOnlyList<BodyPreference>? prefs)
    {
        // WP7 refuses an item with an unsolicited body, wedging the collection in a sync loop;
        // client fetches the body on demand via ItemOperations instead
        if (prefs is null or { Count: 0 }) return null;

        var pref = SelectPreference(prefs, e);
        if (pref is null) return null;

        var native = NativeType(e);
        var target = pref.Type;

        // MIME was asked for but nothing is stored: fall back to the body in its native form
        if (target == BodyType.MIME && !e.HasMimeRaw)
        {
            target = native;
            pref = prefs.FirstOrDefault(p => p.Type == native) ?? pref;
        }

        var data = Convert(e, native, target);

        // an item with no body syncs with no Body element rather than an empty <Data/> with
        // EstimatedDataSize 0, which WP7 rejects
        if (string.IsNullOrWhiteSpace(data)) return null;

        // the type actually produced, which is not necessarily the native one
        var body = new AirSyncBody { Type = target, EstimatedDataSize = BodyConverter.ByteLength(data) };

        if (pref.TruncationSize is { } size && BodyConverter.ByteLength(data) > size)
        {
            if (pref.AllOrNone is 1)
            {
                body.Data = null;
                body.Truncated = 1;
                return body;
            }

            body.Data = BodyConverter.TruncateToBytes(data, size);
            body.Truncated = 1;
        }
        else
        {
            body.Data = data;
        }

        // MS-ASAIRS 2.2.2.35.1: Preview is not returned for an untruncated plain-text body
        if (pref.Preview is { } previewLen and > 0 && !(target == BodyType.PlainText && body.Truncated is null))
            body.Preview = BodyConverter.TruncateToBytes(data, previewLen);

        return body;
    }

    // derived from the normalised thread topic so replies/forwards (sharing a topic)
    // collapse into one conversation; falls back to the subject
    private static byte[] DeriveConversationId(EmailItem e)
    {
        var key = NormaliseTopic(e.HasThreadTopic ? e.ThreadTopic : e.HasSubject ? e.Subject : null);
        if (key is null)
            return Guid.NewGuid().ToByteArray();
        return System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
    }

    // minimal header: reserved 0x01, 5-byte current FILETIME, then the conversation id.
    // reply chains would append 5-byte child blocks; left for later
    private static byte[] BuildConversationIndex(byte[] conversationId, DateTime time)
    {
        var buf = new byte[22];
        buf[0] = 0x01;
        long fileTime = DateTime.SpecifyKind(time, DateTimeKind.Utc).ToFileTimeUtc();
        // high 5 bytes of the 64-bit FILETIME, big-endian
        for (int i = 0; i < 5; i++)
            buf[1 + i] = (byte)(fileTime >> (8 * (7 - i)));
        Array.Copy(conversationId, 0, buf, 6, 16);
        return buf;
    }

    private static string? NormaliseTopic(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return null;
        var s = topic.Trim();
        // strip leading RE:/FW:/FWD: prefixes, repeatedly, case-insensitively
        bool stripped;
        do
        {
            stripped = false;
            foreach (var p in (ReadOnlySpan<string>)["re:", "fw:", "fwd:"])
            {
                if (s.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    s = s[p.Length..].TrimStart();
                    stripped = true;
                }
            }
        } while (stripped);
        return s.Length == 0 ? null : s.ToLowerInvariant();
    }

    private static Models.EmailFlag ToFlagDto(ReLiveWP.Services.Grpc.Mailbox.EmailFlag f)
    {
        var dto = new Models.EmailFlag
        {
            Status = f.HasStatus ? (byte)f.Status : null,
            FlagType = f.HasFlagType ? f.FlagType : null,
            Subject = f.HasSubject ? f.Subject : null,
            DateCompleted = f.DateCompleted != null ? f.DateCompleted.ToDateTime() : null,
            CompleteTime = f.CompleteTime != null ? f.CompleteTime.ToDateTime() : null,
            StartDate = f.StartDate != null ? f.StartDate.ToDateTime() : null,
            DueDate = f.DueDate != null ? f.DueDate.ToDateTime() : null,
            UtcStartDate = f.UtcStartDate != null ? f.UtcStartDate.ToDateTime() : null,
            UtcDueDate = f.UtcDueDate != null ? f.UtcDueDate.ToDateTime() : null,
            ReminderSet = f.HasReminderSet ? (f.ReminderSet ? (byte)1 : (byte)0) : null,
            ReminderTime = f.ReminderTime != null ? f.ReminderTime.ToDateTime() : null,
        };
        return dto;
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
