using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Models;

[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class TaskData
{
    [XmlElement("Subject", Namespace = Constants.Tasks)]
    public string? Subject { get; set; }

    // 0=Low 1=Normal 2=High
    [XmlElement("Importance", Namespace = Constants.Tasks)]
    public byte? Importance { get; set; }

    // 0=Normal 1=Personal 2=Private 3=Confidential
    [XmlElement("Sensitivity", Namespace = Constants.Tasks)]
    public byte? Sensitivity { get; set; }

    [XmlIgnore]
    public DateTime? StartDate { get; set; }

    [XmlElement("StartDate", Namespace = Constants.Tasks)]
    public string? StartDateXml
    {
        get => EasDateHelper.FromDateTime(StartDate);
        set => StartDate = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? UtcStartDate { get; set; }

    [XmlElement("UtcStartDate", Namespace = Constants.Tasks)]
    public string? UtcStartDateXml
    {
        get => EasDateHelper.FromDateTime(UtcStartDate);
        set => UtcStartDate = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? DueDate { get; set; }

    [XmlElement("DueDate", Namespace = Constants.Tasks)]
    public string? DueDateXml
    {
        get => EasDateHelper.FromDateTime(DueDate);
        set => DueDate = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? UtcDueDate { get; set; }

    [XmlElement("UtcDueDate", Namespace = Constants.Tasks)]
    public string? UtcDueDateXml
    {
        get => EasDateHelper.FromDateTime(UtcDueDate);
        set => UtcDueDate = EasDateHelper.ToDateTime(value);
    }

    // 0=not completed 1=completed
    [XmlElement("Complete", Namespace = Constants.Tasks)]
    public byte? Complete { get; set; }

    // required in a response when Complete=1
    [XmlIgnore]
    public DateTime? DateCompleted { get; set; }

    [XmlElement("DateCompleted", Namespace = Constants.Tasks)]
    public string? DateCompletedXml
    {
        get => EasDateHelper.FromDateTime(DateCompleted);
        set => DateCompleted = EasDateHelper.ToDateTime(value);
    }

    // 0=no reminder 1=reminder set
    [XmlElement("ReminderSet", Namespace = Constants.Tasks)]
    public byte? ReminderSet { get; set; }

    [XmlIgnore]
    public DateTime? ReminderTime { get; set; }

    [XmlElement("ReminderTime", Namespace = Constants.Tasks)]
    public string? ReminderTimeXml
    {
        get => EasDateHelper.FromDateTime(ReminderTime);
        set => ReminderTime = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    [XmlElement("Recurrence", Namespace = Constants.Tasks)]
    public TaskRecurrence? Recurrence { get; set; }
}

public class TaskRecurrence
{
    // 0=Daily 1=Weekly 2=Monthly 3=MonthlyNth 5=Yearly 6=YearlyNth
    [XmlElement("Type", Namespace = Constants.Tasks)]
    public byte? Type { get; set; }

    [XmlIgnore]
    public DateTime? Start { get; set; }

    [XmlElement("Start", Namespace = Constants.Tasks)]
    public string? StartXml
    {
        get => EasDateHelper.FromDateTime(Start);
        set => Start = EasDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? Until { get; set; }

    [XmlElement("Until", Namespace = Constants.Tasks)]
    public string? UntilXml
    {
        get => EasDateHelper.FromDateTime(Until);
        set => Until = EasDateHelper.ToDateTime(value);
    }

    // mutually exclusive with Until
    [XmlElement("Occurrences", Namespace = Constants.Tasks)]
    public byte? Occurrences { get; set; }

    [XmlElement("Interval", Namespace = Constants.Tasks)]
    public ushort? Interval { get; set; }

    // bitmask: 1=Sun 2=Mon 4=Tue 8=Wed 16=Thu 32=Fri 64=Sat; 127=last day
    [XmlElement("DayOfWeek", Namespace = Constants.Tasks)]
    public byte? DayOfWeek { get; set; }

    [XmlElement("DayOfMonth", Namespace = Constants.Tasks)]
    public byte? DayOfMonth { get; set; }

    [XmlElement("WeekOfMonth", Namespace = Constants.Tasks)]
    public byte? WeekOfMonth { get; set; }

    [XmlElement("MonthOfYear", Namespace = Constants.Tasks)]
    public byte? MonthOfYear { get; set; }

    // 0=do not regenerate 1=regenerate after completion
    [XmlElement("Regenerate", Namespace = Constants.Tasks)]
    public byte? Regenerate { get; set; }

    [XmlElement("DeadOccur", Namespace = Constants.Tasks)]
    public byte? DeadOccur { get; set; }

    [XmlElement("CalendarType", Namespace = Constants.Tasks)]
    public byte? CalendarType { get; set; }

    [XmlElement("IsLeapMonth", Namespace = Constants.Tasks)]
    public byte? IsLeapMonth { get; set; }

    // 0=Sun ... 6=Sat
    [XmlElement("FirstDayOfWeek", Namespace = Constants.Tasks)]
    public byte? FirstDayOfWeek { get; set; }
}
