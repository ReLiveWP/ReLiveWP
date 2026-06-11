namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public abstract class DbItem
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DbFolder Collection { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

public class DbTask : DbItem { }

public class DbEmail : DbItem
{
    // Addressing
    public string? To { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? From { get; set; }
    public string? ReplyTo { get; set; }
    public string? DisplayTo { get; set; }
    public string? Sender { get; set; }

    // Headers
    public string? Subject { get; set; }
    public DateTime? DateReceived { get; set; }
    public string? ThreadTopic { get; set; }
    public byte? Importance { get; set; }
    public bool? Read { get; set; }
    public string? MessageClass { get; set; }
    public string? InternetCPID { get; set; }
    public string? ContentClass { get; set; }

    // Conversation (opaque BLOBs)
    public byte[]? ConversationId { get; set; }
    public byte[]? ConversationIndex { get; set; }

    // Verb tracking
    public int? LastVerbExecuted { get; set; }
    public DateTime? LastVerbExecutionTime { get; set; }

    // Body
    public string? Body { get; set; }
    public byte? BodyType { get; set; }
    public byte? NativeBodyType { get; set; }
    public string? MimeRaw { get; set; }

    // Flag (MS-ASEMAIL §2.2.2.34). FlagStatus != null marks a flag as present.
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
}

public class DbItemEvent : IDbChangeEvent
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public DbChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}
