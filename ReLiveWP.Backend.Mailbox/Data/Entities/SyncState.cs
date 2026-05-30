namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public class DbSyncState
{
    public const string FolderHierarchyCollectionId = "0";

    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string SyncKey { get; set; } = "0";
    public long Watermark { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string? CachedAnnotationNames { get; set; }
}
