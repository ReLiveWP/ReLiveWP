using System.Globalization;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Calendar = "Calendar";
}

[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class CalendarData
{
    [XmlElement("Timezone", Namespace = Constants.Calendar)]
    public string? Timezone { get; set; }

    [XmlIgnore]
    public DateTime? StartTime { get; set; }

    [XmlElement("StartTime", Namespace = Constants.Calendar)]
    public string? StartTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(StartTime);
        set => StartTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? EndTime { get; set; }

    [XmlElement("EndTime", Namespace = Constants.Calendar)]
    public string? EndTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(EndTime);
        set => EndTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? DtStamp { get; set; }

    [XmlElement("DtStamp", Namespace = Constants.Calendar)]
    public string? DtStampXml
    {
        get => EasDateHelper.FromDateTimeCompact(DtStamp);
        set => DtStamp = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("UID", Namespace = Constants.Calendar)]
    public string? Uid { get; set; }

    [XmlElement("ClientUid", Namespace = Constants.Calendar)]
    public string? ClientUid { get; set; }

    [XmlElement("Subject", Namespace = Constants.Calendar)]
    public string? Subject { get; set; }

    [XmlElement("Location", Namespace = Constants.Calendar)]
    public string? Location { get; set; }

    [XmlElement("Location", Namespace = Constants.AirSyncBase)]
    public AirSyncLocation? LocationEx { get; set; }

    [XmlElement("Reminder", Namespace = Constants.Calendar)]
    public uint? Reminder { get; set; }

    [XmlElement("AllDayEvent", Namespace = Constants.Calendar)]
    public byte? AllDayEvent { get; set; }

    // 0=Free 1=Tentative 2=Busy 3=OOF 4=WorkingElsewhere
    [XmlElement("BusyStatus", Namespace = Constants.Calendar)]
    public byte? BusyStatus { get; set; }

    // 0=Normal 1=Personal 2=Private 3=Confidential
    [XmlElement("Sensitivity", Namespace = Constants.Calendar)]
    public byte? Sensitivity { get; set; }

    // bitmask: bit0=Meeting bit1=ReceivedFromOther bit2=Cancelled
    [XmlElement("MeetingStatus", Namespace = Constants.Calendar)]
    public byte? MeetingStatus { get; set; }

    [XmlElement("OrganizerName", Namespace = Constants.Calendar)]
    public string? OrganizerName { get; set; }

    [XmlElement("OrganizerEmail", Namespace = Constants.Calendar)]
    public string? OrganizerEmail { get; set; }

    [XmlIgnore]
    public DateTime? AppointmentReplyTime { get; set; }

    [XmlElement("AppointmentReplyTime", Namespace = Constants.Calendar)]
    public string? AppointmentReplyTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(AppointmentReplyTime);
        set => AppointmentReplyTime = EasDateHelper.ToDateTime(value);
    }

    // 0=None 1=Organizer 2=Tentative 3=Accepted 4=Declined 5=NotResponded
    [XmlElement("ResponseType", Namespace = Constants.Calendar)]
    public uint? ResponseType { get; set; }

    [XmlElement("ResponseRequested", Namespace = Constants.Calendar)]
    public int? ResponseRequested { get; set; }

    [XmlElement("DisallowNewTimeProposal", Namespace = Constants.Calendar)]
    public int? DisallowNewTimeProposal { get; set; }

    [XmlElement("OnlineMeetingConfLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingConfLink { get; set; }

    [XmlElement("OnlineMeetingExternalLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingExternalLink { get; set; }

    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    [XmlElement("Body", Namespace = Constants.Calendar)]
    public string? BodyLegacy { get; set; }

    [XmlElement("BodyTruncated", Namespace = Constants.Calendar)]
    public int? BodyTruncated { get; set; }

    [XmlElement("Attendees", Namespace = Constants.Calendar)]
    public CalendarAttendees? Attendees { get; set; }

    [XmlElement("Categories", Namespace = Constants.Calendar)]
    public CalendarCategories? Categories { get; set; }

    [XmlElement("Recurrence", Namespace = Constants.Calendar)]
    public CalendarRecurrence? Recurrence { get; set; }

    [XmlElement("Exceptions", Namespace = Constants.Calendar)]
    public CalendarExceptions? Exceptions { get; set; }
}

public class CalendarAttendees
{
    [XmlElement("Attendee", Namespace = Constants.Calendar)]
    public List<CalendarAttendeeData> Items { get; set; } = [];
}

public class CalendarAttendeeData
{
    [XmlElement("Email", Namespace = Constants.Calendar)]
    public string? Email { get; set; }

    [XmlElement("Name", Namespace = Constants.Calendar)]
    public string? Name { get; set; }

    // 0=Unknown 2=Tentative 3=Accept 4=Decline 5=NotResponded
    [XmlElement("AttendeeStatus", Namespace = Constants.Calendar)]
    public byte? AttendeeStatus { get; set; }

    // 1=Required 2=Optional 3=Resource
    [XmlElement("AttendeeType", Namespace = Constants.Calendar)]
    public byte? AttendeeType { get; set; }
}

public class CalendarCategories
{
    [XmlElement("Category", Namespace = Constants.Calendar)]
    public List<string> Items { get; set; } = [];
}

public class CalendarRecurrence
{
    // 0=Daily 1=Weekly 2=Monthly 3=MonthlyNth 5=Yearly 6=YearlyNth
    [XmlElement("Type", Namespace = Constants.Calendar)]
    public byte? Type { get; set; }

    // mutually exclusive with Until
    [XmlElement("Occurrences", Namespace = Constants.Calendar)]
    public ushort? Occurrences { get; set; }

    [XmlElement("Interval", Namespace = Constants.Calendar)]
    public ushort? Interval { get; set; }

    [XmlElement("WeekOfMonth", Namespace = Constants.Calendar)]
    public byte? WeekOfMonth { get; set; }

    // bitmask: 1=Sun 2=Mon 4=Tue 8=Wed 16=Thu 32=Fri 64=Sat
    [XmlElement("DayOfWeek", Namespace = Constants.Calendar)]
    public ushort? DayOfWeek { get; set; }

    [XmlElement("MonthOfYear", Namespace = Constants.Calendar)]
    public byte? MonthOfYear { get; set; }

    // mutually exclusive with Occurrences
    [XmlElement("Until", Namespace = Constants.Calendar)]
    public string? Until { get; set; }

    [XmlElement("DayOfMonth", Namespace = Constants.Calendar)]
    public byte? DayOfMonth { get; set; }

    [XmlElement("CalendarType", Namespace = Constants.Calendar)]
    public byte? CalendarType { get; set; }

    [XmlElement("IsLeapMonth", Namespace = Constants.Calendar)]
    public byte? IsLeapMonth { get; set; }

    // 0=Sun ... 6=Sat
    [XmlElement("FirstDayOfWeek", Namespace = Constants.Calendar)]
    public byte? FirstDayOfWeek { get; set; }
}

public class CalendarExceptions
{
    [XmlElement("Exception", Namespace = Constants.Calendar)]
    public List<CalendarExceptionData> Items { get; set; } = [];
}

public class CalendarExceptionData
{
    [XmlElement("Deleted", Namespace = Constants.Calendar)]
    public byte? Deleted { get; set; }

    [XmlElement("ExceptionStartTime", Namespace = Constants.Calendar)]
    public string? ExceptionStartTime { get; set; }

    [XmlElement("InstanceId", Namespace = Constants.AirSyncBase)]
    public string? InstanceId { get; set; }

    [XmlElement("Subject", Namespace = Constants.Calendar)]
    public string? Subject { get; set; }

    [XmlIgnore]
    public DateTime? StartTime { get; set; }

    [XmlElement("StartTime", Namespace = Constants.Calendar)]
    public string? StartTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(StartTime);
        set => StartTime = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? EndTime { get; set; }

    [XmlElement("EndTime", Namespace = Constants.Calendar)]
    public string? EndTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(EndTime);
        set => EndTime = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("Location", Namespace = Constants.Calendar)]
    public string? Location { get; set; }

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
        get => EasDateHelper.FromDateTimeCompact(DtStamp);
        set => DtStamp = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("MeetingStatus", Namespace = Constants.Calendar)]
    public byte? MeetingStatus { get; set; }

    [XmlIgnore]
    public DateTime? AppointmentReplyTime { get; set; }

    [XmlElement("AppointmentReplyTime", Namespace = Constants.Calendar)]
    public string? AppointmentReplyTimeXml
    {
        get => EasDateHelper.FromDateTimeCompact(AppointmentReplyTime);
        set => AppointmentReplyTime = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("ResponseType", Namespace = Constants.Calendar)]
    public uint? ResponseType { get; set; }

    [XmlElement("OnlineMeetingConfLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingConfLink { get; set; }

    [XmlElement("OnlineMeetingExternalLink", Namespace = Constants.Calendar)]
    public string? OnlineMeetingExternalLink { get; set; }

    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    [XmlElement("Body", Namespace = Constants.Calendar)]
    public string? BodyLegacy { get; set; }

    [XmlElement("Attendees", Namespace = Constants.Calendar)]
    public CalendarAttendees? Attendees { get; set; }

    [XmlElement("Categories", Namespace = Constants.Calendar)]
    public CalendarCategories? Categories { get; set; }
}
