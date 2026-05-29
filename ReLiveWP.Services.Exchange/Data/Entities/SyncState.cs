namespace ReLiveWP.Services.Exchange.Data.Entities;

// Per-device, per-collection sync cursor. Folder hierarchy uses FolderHierarchyCollectionId;
// item collections use the folder's ServerId.
public class SyncState
{
    public const string FolderHierarchyCollectionId = "0";

    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string SyncKey { get; set; } = "0";
    public long Watermark { get; set; }        // -1 = first real sync pending (items only)
    public DateTime LastSeenAt { get; set; }
}
