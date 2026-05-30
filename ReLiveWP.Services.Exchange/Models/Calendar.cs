using System.Globalization;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Calendar = "Calendar";
}

// Represents the content of an airsync:ApplicationData element for the Calendar class.
[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class CalendarData
{
    // ── Timing ────────────────────────────────────────────────────────────────
    // Timezone: base64-encoded TimeZone structure. Absent for AllDayEvent in v16.0+.
    [XmlElement("Timezone", Namespace = Constants.Calendar)]
    public string? Timezone { get; set; }

    [XmlIgnore]
    public DateTime? StartTime { get; set; }

    [XmlElement("StartTime", Namespace = Constants.Calendar)]
    public string? StartTimeXml
    {
        get => EasDateHelper.FromDateTime(StartTime);
        set => StartTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? EndTime { get; set; }

    [XmlElement("EndTime", Namespace = Constants.Calendar)]
    public string? EndTimeXml
    {
        get => EasDateHelper.FromDateTime(EndTime);
        set => EndTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? DtStamp { get; set; }

    [XmlElement("DtStamp", Namespace = Constants.Calendar)]
    public string? DtStampXml
    {
        get => EasDateHelper.FromDateTime(DtStamp);
        set => DtStamp = EasDateHelper.ToDateTime(value);
    }

    // ── Identity ──────────────────────────────────────────────────────────────
    [XmlElement("UID", Namespace = Constants.Calendar)]
    public string? Uid { get; set; }

    // ClientUid: random client-generated ID; v16.0+ only; requests only.
    [XmlElement("ClientUid", Namespace = Constants.Calendar)]
    public string? ClientUid { get; set; }

    // ── Meeting info ──────────────────────────────────────────────────────────
    [XmlElement("Subject", Namespace = Constants.Calendar)]
    public string? Subject { get; set; }

    // calendar:Location — plain string; v2.5–14.1. Replaced by airsyncbase:Location in v16.0+.
    [XmlElement("Location", Namespace = Constants.Calendar)]
    public string? Location { get; set; }

    // airsyncbase:Location — rich structured location; v16.0+ only.
    [XmlElement("Location", Namespace = Constants.AirSyncBase)]
    public AirSyncLocation? LocationEx { get; set; }

    // Reminder: minutes before StartTime; xs:unsignedInt. Can be empty tag in v16.0+ to clear.
    [XmlElement("Reminder", Namespace = Constants.Calendar)]
    public uint? Reminder { get; set; }

    // xs:unsignedByte; 0=not all-day 1=all-day.
    [XmlElement("AllDayEvent", Namespace = Constants.Calendar)]
    public byte? AllDayEvent { get; set; }

    // xs:unsignedByte; 0=Free 1=Tentative 2=Busy 3=OOF 4=WorkingElsewhere.
    [XmlElement("BusyStatus", Namespace = Constants.Calendar)]
    public byte? BusyStatus { get; set; }

    // xs:unsignedByte; 0=Normal 1=Personal 2=Private 3=Confidential.
    [XmlElement("Sensitivity", Namespace = Constants.Calendar)]
    public byte? Sensitivity { get; set; }

    // xs:unsignedByte bitmask; bit0=Meeting bit1=ReceivedFromOther bit2=Cancelled.
    [XmlElement("MeetingStatus", Namespace = Constants.Calendar)]
    public byte? MeetingStatus { get; set; }

    [XmlElement("OrganizerName", Namespace = Constants.Calendar)]
    public string? OrganizerName { get; set; }

    [XmlElement("OrganizerEmail", Namespace = Constants.Calendar)]
    public string? OrganizerEmail { get; set; }

    // ── Meeting response ──────────────────────────────────────────────────────
    // AppointmentReplyTime: response only; MUST NOT be in requests.
    [XmlIgnore]
    public DateTime? AppointmentReplyTime { get; set; }

    [XmlElement("AppointmentReplyTime", Namespace = Constants.Calendar)]
    public string? AppointmentReplyTimeXml
    {
        get => EasDateHelper.FromDateTime(AppointmentReplyTime);
        set => AppointmentReplyTime = EasDateHelper.ToDateTime(value);
    }

    // xs:unsignedInt; 0=None 1=Organizer 2=Tentative 3=Accepted 4=Declined 5=NotResponded.
    [XmlElement("ResponseType", Namespace = Constants.Calendar)]
    public uint? ResponseType { get; set; }

    // xs:boolean; EAS 0/1 encoding.
    [XmlElement("ResponseRequested", Namespace = Constants.Calendar)]
    public int? ResponseRequested { get; set; }

    // xs:boolean; EAS 0/1 encoding.
    [XmlElement("DisallowNewTimeProposal", Namespace = Constants.Calendar)]
    public int? DisallowNewTimeProposal { get; set; }

    // ── Online meeting ────────────────────────────────────────────────────────
    // Response only; MUST NOT be in requests.
    [XmlElement("OnlineMeetingConfLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingConfLink { get; set; }

    [XmlElement("OnlineMeetingExternalLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingExternalLink { get; set; }

    // ── Body (protocol 12.0+) ─────────────────────────────────────────────────
    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    // ── Body (protocol 2.5 only) ──────────────────────────────────────────────
    [XmlElement("Body", Namespace = Constants.Calendar)]
    public string? BodyLegacy { get; set; }

    // xs:boolean; EAS 0/1 encoding; v2.5 only.
    [XmlElement("BodyTruncated", Namespace = Constants.Calendar)]
    public int? BodyTruncated { get; set; }

    // ── Collections ───────────────────────────────────────────────────────────
    [XmlElement("Attendees", Namespace = Constants.Calendar)]
    public CalendarAttendees? Attendees { get; set; }

    [XmlElement("Categories", Namespace = Constants.Calendar)]
    public CalendarCategories? Categories { get; set; }

    [XmlElement("Recurrence", Namespace = Constants.Calendar)]
    public CalendarRecurrence? Recurrence { get; set; }

    [XmlElement("Exceptions", Namespace = Constants.Calendar)]
    public CalendarExceptions? Exceptions { get; set; }

    // Conversions: see Models/ProtoExtensions.cs for ToProtoCalendar().
}

// ── <calendar:Attendees> container ────────────────────────────────────────────
public class CalendarAttendees
{
    [XmlElement("Attendee", Namespace = Constants.Calendar)]
    public List<CalendarAttendeeData> Items { get; set; } = [];
}

public class CalendarAttendeeData
{
    [XmlElement("Email", Namespace = Constants.Calendar)]
    public string Email { get; set; } = null!;

    [XmlElement("Name", Namespace = Constants.Calendar)]
    public string Name { get; set; } = null!;

    // xs:unsignedByte; 0=Unknown 2=Tentative 3=Accept 4=Decline 5=NotResponded.
    [XmlElement("AttendeeStatus", Namespace = Constants.Calendar)]
    public byte? AttendeeStatus { get; set; }

    // xs:unsignedByte; 1=Required 2=Optional 3=Resource.
    [XmlElement("AttendeeType", Namespace = Constants.Calendar)]
    public byte? AttendeeType { get; set; }
}

// ── <calendar:Categories> container ──────────────────────────────────────────
public class CalendarCategories
{
    [XmlElement("Category", Namespace = Constants.Calendar)]
    public List<string> Items { get; set; } = [];
}

// ── <calendar:Recurrence> ─────────────────────────────────────────────────────
public class CalendarRecurrence
{
    // xs:unsignedByte; 0=Daily 1=Weekly 2=Monthly 3=MonthlyNth 5=Yearly 6=YearlyNth.
    [XmlElement("Type", Namespace = Constants.Calendar)]
    public byte? Type { get; set; }

    // xs:unsignedShort; max 999. Mutually exclusive with Until.
    [XmlElement("Occurrences", Namespace = Constants.Calendar)]
    public ushort? Occurrences { get; set; }

    // xs:unsignedShort; max 999.
    [XmlElement("Interval", Namespace = Constants.Calendar)]
    public ushort? Interval { get; set; }

    // xs:unsignedByte; 1–5. Required when Type=3 or 6.
    [XmlElement("WeekOfMonth", Namespace = Constants.Calendar)]
    public byte? WeekOfMonth { get; set; }

    // xs:unsignedShort bitmask; 1=Sun 2=Mon 4=Tue 8=Wed 16=Thu 32=Fri 64=Sat; max 127.
    [XmlElement("DayOfWeek", Namespace = Constants.Calendar)]
    public ushort? DayOfWeek { get; set; }

    // xs:unsignedByte; 1–12. Required when Type=5 or 6.
    [XmlElement("MonthOfYear", Namespace = Constants.Calendar)]
    public byte? MonthOfYear { get; set; }

    // Compact DateTime string. Mutually exclusive with Occurrences.
    [XmlElement("Until", Namespace = Constants.Calendar)]
    public string? Until { get; set; }

    // xs:unsignedByte; 1–31. Required when Type=2 or 5.
    [XmlElement("DayOfMonth", Namespace = Constants.Calendar)]
    public byte? DayOfMonth { get; set; }

    // xs:unsignedByte; 0–23. Required when Type is 2, 3, 5, or 6 in responses.
    [XmlElement("CalendarType", Namespace = Constants.Calendar)]
    public byte? CalendarType { get; set; }

    // xs:unsignedByte; 0=False 1=True.
    [XmlElement("IsLeapMonth", Namespace = Constants.Calendar)]
    public byte? IsLeapMonth { get; set; }

    // xs:unsignedByte; 0=Sun … 6=Sat. v14.1+. Server MUST return when Type=1.
    [XmlElement("FirstDayOfWeek", Namespace = Constants.Calendar)]
    public byte? FirstDayOfWeek { get; set; }
}

// ── <calendar:Exceptions> container ──────────────────────────────────────────
public class CalendarExceptions
{
    [XmlElement("Exception", Namespace = Constants.Calendar)]
    public List<CalendarExceptionData> Items { get; set; } = [];
}

// ── <calendar:Exception> ─────────────────────────────────────────────────────
// An exception replaces or deletes a single occurrence in a recurring series.
// ExceptionStartTime identifies the replaced occurrence in v2.5–14.1;
// airsyncbase:InstanceId is used instead in v16.0+.
public class CalendarExceptionData
{
    // xs:unsignedByte; 1 = this occurrence is deleted.
    [XmlElement("Deleted", Namespace = Constants.Calendar)]
    public byte? Deleted { get; set; }

    // Required in v2.5–14.1; absent in v16.0+.
    [XmlElement("ExceptionStartTime", Namespace = Constants.Calendar)]
    public string? ExceptionStartTime { get; set; }

    // Required in v16.0+; replaces ExceptionStartTime.
    [XmlElement("InstanceId", Namespace = Constants.AirSyncBase)]
    public string? InstanceId { get; set; }

    // ── Overriding fields (all optional; inherit from master if absent) ────────
    [XmlElement("Subject", Namespace = Constants.Calendar)]
    public string? Subject { get; set; }

    [XmlIgnore]
    public DateTime? StartTime { get; set; }

    [XmlElement("StartTime", Namespace = Constants.Calendar)]
    public string? StartTimeXml
    {
        get => EasDateHelper.FromDateTime(StartTime);
        set => StartTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? EndTime { get; set; }

    [XmlElement("EndTime", Namespace = Constants.Calendar)]
    public string? EndTimeXml
    {
        get => EasDateHelper.FromDateTime(EndTime);
        set => EndTime = EasDateHelper.ToDateTime(value);
    }

    // calendar:Location — v2.5–14.1.
    [XmlElement("Location", Namespace = Constants.Calendar)]
    public string? Location { get; set; }

    // airsyncbase:Location — v16.0+.
    [XmlElement("Location", Namespace = Constants.AirSyncBase)]
    public AirSyncLocation? LocationEx { get; set; }

    [XmlElement("Sensitivity", Namespace = Constants.Calendar)]
    public byte? Sensitivity { get; set; }

    [XmlElement("BusyStatus", Namespace = Constants.Calendar)]
    public byte? BusyStatus { get; set; }

    [XmlElement("AllDayEvent", Namespace = Constants.Calendar)]
    public byte? AllDayEvent { get; set; }

    [XmlElement("Reminder", Namespace = Constants.Calendar)]
    public uint? Reminder { get; set; }

    [XmlIgnore]
    public DateTime? DtStamp { get; set; }

    [XmlElement("DtStamp", Namespace = Constants.Calendar)]
    public string? DtStampXml
    {
        get => EasDateHelper.FromDateTime(DtStamp);
        set => DtStamp = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("MeetingStatus", Namespace = Constants.Calendar)]
    public byte? MeetingStatus { get; set; }

    [XmlIgnore]
    public DateTime? AppointmentReplyTime { get; set; }

    [XmlElement("AppointmentReplyTime", Namespace = Constants.Calendar)]
    public string? AppointmentReplyTimeXml
    {
        get => EasDateHelper.FromDateTime(AppointmentReplyTime);
        set => AppointmentReplyTime = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("ResponseType", Namespace = Constants.Calendar)]
    public uint? ResponseType { get; set; }

    // Response only; MUST NOT be in requests.
    [XmlElement("OnlineMeetingConfLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingConfLink { get; set; }

    [XmlElement("OnlineMeetingExternalLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingExternalLink { get; set; }

    // airsyncbase:Body — v12.0+.
    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    // calendar:Body — v2.5 only.
    [XmlElement("Body", Namespace = Constants.Calendar)]
    public string? BodyLegacy { get; set; }

    [XmlElement("Attendees", Namespace = Constants.Calendar)]
    public CalendarAttendees? Attendees { get; set; }

    [XmlElement("Categories", Namespace = Constants.Calendar)]
    public CalendarCategories? Categories { get; set; }
}
