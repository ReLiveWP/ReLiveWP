namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public abstract class DbItem
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DbFolder Collection { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    // client-supplied Add id; a retransmitted Add with the same (UserId, CollectionId, ClientId) resolves
    // to this row instead of duplicating
    public string? ClientId { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // set when an item fails validation and can't be auto-corrected; withheld from sync until fixed
    public DateTime? ValidationFlaggedAt { get; set; }
    public string? ValidationReason { get; set; }

    // maps onto the Postgres xmin system column, used for version tracking
    public uint Version { get; set; }
}

public class DbTask : DbItem
{
    public string? Subject { get; set; }
    public byte? Importance { get; set; }
    public byte? Sensitivity { get; set; }
    public string? Notes { get; set; }
    public byte? NativeBodyType { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? UtcStartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? UtcDueDate { get; set; }

    public bool? Complete { get; set; }
    public DateTime? DateCompleted { get; set; }

    public bool? ReminderSet { get; set; }
    public DateTime? ReminderTime { get; set; }

    public byte? RecurrenceType { get; set; }
    public DateTime? RecurrenceStart { get; set; }
    public DateTime? RecurrenceUntil { get; set; }
    public byte? RecurrenceOccurrences { get; set; }
    public ushort? RecurrenceInterval { get; set; }
    public byte? RecurrenceDayOfWeek { get; set; }
    public byte? RecurrenceDayOfMonth { get; set; }
    public byte? RecurrenceWeekOfMonth { get; set; }
    public byte? RecurrenceMonthOfYear { get; set; }
    public bool? RecurrenceRegenerate { get; set; }
    public byte? RecurrenceDeadOccur { get; set; }
    public byte? RecurrenceCalendarType { get; set; }
    public bool? RecurrenceIsLeapMonth { get; set; }
    public byte? RecurrenceFirstDayOfWeek { get; set; }
}

public class DbNote : DbItem
{
    public string? Subject { get; set; }
    public string? MessageClass { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? Body { get; set; }
    public byte? NativeBodyType { get; set; }

    public List<DbNoteCategory> Categories { get; set; } = [];
}

public class DbNoteCategory
{
    public string Id { get; set; } = null!;
    public string NoteItemId { get; set; } = null!;
    public DbNote NoteItem { get; set; } = null!;
    public string Category { get; set; } = null!;
}

public class DbItemEvent : IDbChangeEvent
{
    public long Id { get; set; }
    public long CommitId { get; set; }
    public string UserId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public DbChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}
