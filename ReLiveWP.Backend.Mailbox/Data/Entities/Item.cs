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

public class DbEmail : DbItem { }

public class DbItemEvent : IDbChangeEvent
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public DbChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}
