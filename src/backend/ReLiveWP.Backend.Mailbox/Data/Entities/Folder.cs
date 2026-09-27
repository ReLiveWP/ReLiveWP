namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public enum DbFolderType
{
    Generic = 1,
    InboxDefault,
    DraftsDefault,
    DeletedItemsDefault,
    SentItemsDefault,
    OutboxDefault,
    TasksDefault,
    CalendarDefault,
    ContactsDefault,
    NotesDefault,
    JournalDefault,
    Mail,
    Calendar,
    Contacts,
    Task,
    Journal,
    Notes,
    Unknown,
    RecipientInformationCache,
    MeContact = 27,
}

public class DbFolder
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ParentServerId { get; set; } = "0";
    public string DisplayName { get; set; } = null!;
    public DbFolderType Type { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<DbItem> FolderItems { get; set; } = new List<DbItem>();
    public string? SourceId { get; set; }
    public string? AccountName { get; set; }
    public bool IsHidden { get; set; } = false;
}

public class DbFolderEvent : IDbChangeEvent
{
    public long Id { get; set; }
    public long CommitId { get; set; }
    public string UserId { get; set; } = null!;
    public DbChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public string? ParentServerId { get; set; }
    public string? DisplayName { get; set; }
    public DbFolderType? FolderType { get; set; }
    public DateTime OccurredAt { get; set; }
}
