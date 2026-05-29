namespace ReLiveWP.Services.Exchange.Data.Entities;

public enum ChangeEventType { Add, Update, Delete }

// Append-only change log row; Id is the watermark a device syncs against.
public interface IChangeEvent
{
    long Id { get; }
    string ServerId { get; }
    ChangeEventType EventType { get; }
}
