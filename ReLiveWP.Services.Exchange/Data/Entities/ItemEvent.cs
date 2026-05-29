namespace ReLiveWP.Services.Exchange.Data.Entities;

public class ItemEvent : IChangeEvent
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public ChangeEventType EventType { get; set; }
    public string ServerId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}
