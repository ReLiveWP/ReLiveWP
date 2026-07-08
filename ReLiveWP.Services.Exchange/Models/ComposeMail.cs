using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string ComposeMail = "ComposeMail";
}

// MS-ASCMD §2.2.3.162 / §6.36 — SendMail request (protocol 14.x WBXML form).
// The Mime element carries the raw RFC822 message as opaque WBXML data.
[XmlRoot("SendMail", Namespace = Constants.ComposeMail)]
public class SendMailRequest
{
    [XmlElement("ClientId", Namespace = Constants.ComposeMail)]
    public string? ClientId { get; set; }

    [XmlElement("AccountId", Namespace = Constants.ComposeMail)]
    public string? AccountId { get; set; }

    // Empty element; presence (non-null) means "save a copy in Sent Items".
    [XmlElement("SaveInSentItems", Namespace = Constants.ComposeMail)]
    public string? SaveInSentItems { get; set; }

    [XmlElement("Mime", Namespace = Constants.ComposeMail)]
    public string? Mime { get; set; }
}

// §6.38 — SmartReply / §6.40 — SmartForward. Both add a required Source referencing the original.
[XmlRoot("SmartReply", Namespace = Constants.ComposeMail)]
public class SmartReplyRequest
{
    [XmlElement("ClientId", Namespace = Constants.ComposeMail)]
    public string? ClientId { get; set; }

    [XmlElement("Source", Namespace = Constants.ComposeMail)]
    public ComposeMailSource? Source { get; set; }

    [XmlElement("AccountId", Namespace = Constants.ComposeMail)]
    public string? AccountId { get; set; }

    [XmlElement("SaveInSentItems", Namespace = Constants.ComposeMail)]
    public string? SaveInSentItems { get; set; }

    [XmlElement("ReplaceMime", Namespace = Constants.ComposeMail)]
    public string? ReplaceMime { get; set; }

    [XmlElement("Mime", Namespace = Constants.ComposeMail)]
    public string? Mime { get; set; }
}

[XmlRoot("SmartForward", Namespace = Constants.ComposeMail)]
public class SmartForwardRequest
{
    [XmlElement("ClientId", Namespace = Constants.ComposeMail)]
    public string? ClientId { get; set; }

    [XmlElement("Source", Namespace = Constants.ComposeMail)]
    public ComposeMailSource? Source { get; set; }

    [XmlElement("AccountId", Namespace = Constants.ComposeMail)]
    public string? AccountId { get; set; }

    [XmlElement("SaveInSentItems", Namespace = Constants.ComposeMail)]
    public string? SaveInSentItems { get; set; }

    [XmlElement("ReplaceMime", Namespace = Constants.ComposeMail)]
    public string? ReplaceMime { get; set; }

    [XmlElement("Mime", Namespace = Constants.ComposeMail)]
    public string? Mime { get; set; }
}

// §2.2.3.176 — Source: FolderId+ItemId identify the original message; or LongId alone.
public class ComposeMailSource
{
    [XmlElement("FolderId", Namespace = Constants.ComposeMail)]
    public string? FolderId { get; set; }

    [XmlElement("ItemId", Namespace = Constants.ComposeMail)]
    public string? ItemId { get; set; }

    [XmlElement("LongId", Namespace = Constants.ComposeMail)]
    public string? LongId { get; set; }

    [XmlElement("InstanceId", Namespace = Constants.ComposeMail)]
    public string? InstanceId { get; set; }
}

// Error responses. On success the server returns an empty HTTP 200 (no body).
[XmlRoot("SendMail", Namespace = Constants.ComposeMail)]
public class SendMailResponse
{
    [XmlElement("Status", Namespace = Constants.ComposeMail)]
    public int Status { get; set; }
}

[XmlRoot("SmartReply", Namespace = Constants.ComposeMail)]
public class SmartReplyResponse
{
    [XmlElement("Status", Namespace = Constants.ComposeMail)]
    public int Status { get; set; }
}

[XmlRoot("SmartForward", Namespace = Constants.ComposeMail)]
public class SmartForwardResponse
{
    [XmlElement("Status", Namespace = Constants.ComposeMail)]
    public int Status { get; set; }
}
