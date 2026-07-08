using System.Globalization;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Email = "Email";
    public const string Email2 = "Email2";
    public const string Tasks = "Tasks";
}

// Represents the content of an airsync:ApplicationData element for the Email class
// (MS-ASEMAIL). Serialised with XmlSerializer; child elements are injected directly into
// ApplicationData.Elements in Sync/ItemOperations responses.
//
// ConversationId/ConversationIndex (email2) are intentionally not serialised here: they are
// opaque binary BLOBs that require WBXML OPAQUE encoding, which is a follow-up. They are still
// persisted server-side.
[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class EmailData
{
    [XmlElement("To", Namespace = Constants.Email)]
    public string? To { get; set; }

    [XmlElement("Cc", Namespace = Constants.Email)]
    public string? Cc { get; set; }

    // Note: Bcc is not part of the protocol 14.x Email class (it is Email2/16.x only), so it is
    // deliberately not surfaced here even though the backend stores it.

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

    // dateTime on the wire: yyyy-MM-ddTHH:mm:ss.fffZ
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

    // unsignedByte: 0 low, 1 normal, 2 high
    [XmlElement("Importance", Namespace = Constants.Email)]
    public byte? Importance { get; set; }

    // EAS boolean 0|1
    [XmlElement("Read", Namespace = Constants.Email)]
    public byte? Read { get; set; }

    [XmlElement("MessageClass", Namespace = Constants.Email)]
    public string? MessageClass { get; set; }

    [XmlElement("InternetCPID", Namespace = Constants.Email)]
    public string? InternetCPID { get; set; }

    [XmlElement("ContentClass", Namespace = Constants.Email)]
    public string? ContentClass { get; set; }

    // email2 conversation BLOBs. Serialised as base64 text; the WBXML encoder emits them as
    // opaque binary (see ASWBXML.opaqueElements). The client treats these as opaque.
    [XmlElement("ConversationId", Namespace = Constants.Email2)]
    public byte[]? ConversationId { get; set; }

    [XmlElement("ConversationIndex", Namespace = Constants.Email2)]
    public byte[]? ConversationIndex { get; set; }

    // 0 none, 1 reply, 2 reply-all, 3 forward
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

    // airsyncbase:Body is the canonical body element for protocol >= 12.0.
    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    // xs:unsignedByte: 1=PlainText, 2=HTML, 3=RTF, 4=MIME.
    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }
}

// <email:Flag> — follow-up flag (MS-ASEMAIL §2.2.2.34). Children span the email and tasks
// namespaces. An empty Flag (all children null) round-trips as <Flag/> meaning "no flag".
public class EmailFlag
{
    [XmlElement("Subject", Namespace = Constants.Tasks)]
    public string? Subject { get; set; }

    // 0 clear, 1 active, 2 complete
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

// MS-ASDTYPE §2.3 dateTime: yyyy-MM-ddTHH:mm:ss.fffZ (UTC, millisecond precision).
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
