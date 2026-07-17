using System.Globalization;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Email = "Email";
    public const string Email2 = "Email2";
    public const string Tasks = "Tasks";
}

[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class EmailData
{
    [XmlElement("To", Namespace = Constants.Email)]
    public string? To { get; set; }

    [XmlElement("Cc", Namespace = Constants.Email)]
    public string? Cc { get; set; }

    // Bcc deliberately not surfaced: backend stores it but this class doesn't carry it
    [XmlElement("From", Namespace = Constants.Email)]
    public string? From { get; set; }

    [XmlElement("ReplyTo", Namespace = Constants.Email)]
    public string? ReplyTo { get; set; }

    [XmlElement("Sender", Namespace = Constants.Email2)]
    public string? Sender { get; set; }

    [XmlElement("DisplayTo", Namespace = Constants.Email)]
    public string? DisplayTo { get; set; }

    [XmlElement("Subject", Namespace = Constants.Email)]
    public string? Subject { get; set; }

    [XmlIgnore]
    public DateTime? DateReceived { get; set; }

    [XmlElement("DateReceived", Namespace = Constants.Email)]
    public string? DateReceivedXml
    {
        get => EmailDateHelper.FromDateTime(DateReceived);
        set => DateReceived = EmailDateHelper.ToDateTime(value);
    }

    [XmlElement("ThreadTopic", Namespace = Constants.Email)]
    public string? ThreadTopic { get; set; }

    // 0=low 1=normal 2=high
    [XmlElement("Importance", Namespace = Constants.Email)]
    public byte? Importance { get; set; }

    [XmlElement("Read", Namespace = Constants.Email)]
    public byte? Read { get; set; }

    [XmlElement("MessageClass", Namespace = Constants.Email)]
    public string? MessageClass { get; set; }

    [XmlElement("InternetCPID", Namespace = Constants.Email)]
    public string? InternetCPID { get; set; }

    [XmlElement("ContentClass", Namespace = Constants.Email)]
    public string? ContentClass { get; set; }

    // serialised as base64 text; ASWBXML re-encodes these as opaque binary on the wire
    [XmlElement("ConversationId", Namespace = Constants.Email2)]
    public byte[]? ConversationId { get; set; }

    [XmlElement("ConversationIndex", Namespace = Constants.Email2)]
    public byte[]? ConversationIndex { get; set; }

    // 0=none 1=reply 2=reply-all 3=forward
    [XmlElement("LastVerbExecuted", Namespace = Constants.Email2)]
    public int? LastVerbExecuted { get; set; }

    [XmlIgnore]
    public DateTime? LastVerbExecutionTime { get; set; }

    [XmlElement("LastVerbExecutionTime", Namespace = Constants.Email2)]
    public string? LastVerbExecutionTimeXml
    {
        get => EmailDateHelper.FromDateTime(LastVerbExecutionTime);
        set => LastVerbExecutionTime = EmailDateHelper.ToDateTime(value);
    }

    [XmlElement("Flag", Namespace = Constants.Email)]
    public EmailFlag? Flag { get; set; }

    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    // 1=PlainText 2=HTML 3=RTF 4=MIME
    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    [XmlElement("Attachments", Namespace = Constants.AirSyncBase)]
    public AirSyncAttachments? Attachments { get; set; }
}

// MS-ASAIRS 2.2.2.8
public class AirSyncAttachments
{
    [XmlElement("Attachment", Namespace = Constants.AirSyncBase)]
    public List<AirSyncAttachment> Items { get; set; } = [];
}

// MS-ASAIRS 2.2.2.7 (command-response shape); FileReference doubles as the
// server-assigned attachment id used by ItemOperations Fetch / GetAttachment
public class AirSyncAttachment
{
    [XmlElement("DisplayName", Namespace = Constants.AirSyncBase)]
    public string? DisplayName { get; set; }

    [XmlElement("FileReference", Namespace = Constants.AirSyncBase)]
    public string? FileReference { get; set; }

    // 1=normal 5=embedded message 6=OLE; required by spec, always populated here
    [XmlElement("Method", Namespace = Constants.AirSyncBase)]
    public byte Method { get; set; } = 1;

    [XmlElement("EstimatedDataSize", Namespace = Constants.AirSyncBase)]
    public int EstimatedDataSize { get; set; }

    [XmlElement("ContentId", Namespace = Constants.AirSyncBase)]
    public string? ContentId { get; set; }

    [XmlElement("ContentLocation", Namespace = Constants.AirSyncBase)]
    public string? ContentLocation { get; set; }

    // empty-tag element: presence means true, matching Sync's Partial/GetChanges pattern
    [XmlIgnore]
    public bool IsInline { get; set; }

    [XmlElement("IsInline", Namespace = Constants.AirSyncBase)]
    public string? IsInlineXml
    {
        get => IsInline ? string.Empty : null;
        set => IsInline = value != null;
    }
}

// an empty Flag (all children null) round-trips as <Flag/> meaning "no flag"
public class EmailFlag
{
    [XmlElement("Subject", Namespace = Constants.Tasks)]
    public string? Subject { get; set; }

    // 0=clear 1=active 2=complete
    [XmlElement("Status", Namespace = Constants.Email)]
    public byte? Status { get; set; }

    [XmlElement("FlagType", Namespace = Constants.Email)]
    public string? FlagType { get; set; }

    [XmlIgnore]
    public DateTime? DateCompleted { get; set; }

    [XmlElement("DateCompleted", Namespace = Constants.Tasks)]
    public string? DateCompletedXml
    {
        get => EmailDateHelper.FromDateTime(DateCompleted);
        set => DateCompleted = EmailDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? CompleteTime { get; set; }

    [XmlElement("CompleteTime", Namespace = Constants.Email)]
    public string? CompleteTimeXml
    {
        get => EmailDateHelper.FromDateTime(CompleteTime);
        set => CompleteTime = EmailDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? StartDate { get; set; }

    [XmlElement("StartDate", Namespace = Constants.Tasks)]
    public string? StartDateXml
    {
        get => EmailDateHelper.FromDateTime(StartDate);
        set => StartDate = EmailDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? DueDate { get; set; }

    [XmlElement("DueDate", Namespace = Constants.Tasks)]
    public string? DueDateXml
    {
        get => EmailDateHelper.FromDateTime(DueDate);
        set => DueDate = EmailDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? UtcStartDate { get; set; }

    [XmlElement("UtcStartDate", Namespace = Constants.Tasks)]
    public string? UtcStartDateXml
    {
        get => EmailDateHelper.FromDateTime(UtcStartDate);
        set => UtcStartDate = EmailDateHelper.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? UtcDueDate { get; set; }

    [XmlElement("UtcDueDate", Namespace = Constants.Tasks)]
    public string? UtcDueDateXml
    {
        get => EmailDateHelper.FromDateTime(UtcDueDate);
        set => UtcDueDate = EmailDateHelper.ToDateTime(value);
    }

    [XmlElement("ReminderSet", Namespace = Constants.Tasks)]
    public byte? ReminderSet { get; set; }

    [XmlIgnore]
    public DateTime? ReminderTime { get; set; }

    [XmlElement("ReminderTime", Namespace = Constants.Tasks)]
    public string? ReminderTimeXml
    {
        get => EmailDateHelper.FromDateTime(ReminderTime);
        set => ReminderTime = EmailDateHelper.ToDateTime(value);
    }
}

internal static class EmailDateHelper
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

    public static string? FromDateTime(DateTime? dt) =>
        dt?.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);

    public static DateTime? ToDateTime(string? s) =>
        string.IsNullOrEmpty(s) ? null
        : DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d : null;
}
