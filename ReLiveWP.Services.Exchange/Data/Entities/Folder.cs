using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Data.Entities;

public class Folder
{
    public string Id { get; set; } = null!;         // stable EAS ServerId (GUID); never reused
    public string UserId { get; set; } = null!;
    public string ParentServerId { get; set; } = "0"; // "0" for root-level folders
    public string DisplayName { get; set; } = null!;
    public FolderType Type { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<Item> FolderItems { get; set; } = new List<Item>();
    public string? SourceId { get; set; } = null;
    public string? AccountName { get; set; } = null;
    public bool IsHidden { get; set; } = false;
}
