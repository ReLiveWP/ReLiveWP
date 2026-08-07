namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public class DbEmail : DbItem
{
    public string? To { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? From { get; set; }
    public string? ReplyTo { get; set; }
    public string? DisplayTo { get; set; }
    public string? Sender { get; set; }

    public string? Subject { get; set; }
    public DateTime? DateReceived { get; set; }
    public string? ThreadTopic { get; set; }
    public byte? Importance { get; set; }
    public bool? Read { get; set; }
    public string? MessageClass { get; set; }
    public string? InternetCPID { get; set; }
    public string? ContentClass { get; set; }

    public byte[]? ConversationId { get; set; }
    public byte[]? ConversationIndex { get; set; }

    public int? LastVerbExecuted { get; set; }
    public DateTime? LastVerbExecutionTime { get; set; }

    public string? Body { get; set; }
    public byte? BodyType { get; set; }
    public byte? NativeBodyType { get; set; }
    public string? MimeRaw { get; set; }

    // FlagStatus != null marks a flag as present
    public byte? FlagStatus { get; set; }
    public string? FlagType { get; set; }
    public string? FlagSubject { get; set; }
    public DateTime? FlagDateCompleted { get; set; }
    public DateTime? FlagCompleteTime { get; set; }
    public DateTime? FlagStartDate { get; set; }
    public DateTime? FlagDueDate { get; set; }
    public DateTime? FlagUtcStartDate { get; set; }
    public DateTime? FlagUtcDueDate { get; set; }
    public bool? FlagReminderSet { get; set; }
    public DateTime? FlagReminderTime { get; set; }

    public List<DbAttachment> Attachments { get; set; } = [];
}

public class DbAttachment
{
    // doubles as the FileReference value clients use to fetch this attachment
    public string Id { get; set; } = null!;
    public string EmailItemId { get; set; } = null!;
    public DbEmail EmailItem { get; set; } = null!;

    public string? DisplayName { get; set; }
    public string? ContentType { get; set; }
    public int? EstimatedDataSize { get; set; }
    // 1=normal 5=embedded message 6=OLE, per MS-ASAIRS 2.2.2.31.2
    public byte? Method { get; set; }
    public string? ContentId { get; set; }
    public string? ContentLocation { get; set; }
    public bool? IsInline { get; set; }
    public byte[]? Content { get; set; }
}
