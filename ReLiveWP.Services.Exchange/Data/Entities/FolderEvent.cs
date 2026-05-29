using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Data.Entities;

public class FolderEvent : IChangeEvent
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public ChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public string? ParentServerId { get; set; }
    public string? DisplayName { get; set; }
    public FolderType? FolderType { get; set; }
    public DateTime OccurredAt { get; set; }
}
