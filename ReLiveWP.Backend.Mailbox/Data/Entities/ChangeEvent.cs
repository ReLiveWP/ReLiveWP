namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public enum DbChangeEventType { Add, Update, Delete }

public interface IDbChangeEvent
{
    long Id { get; }
    string ServerId { get; }
    DbChangeEventType EventType { get; }
}
