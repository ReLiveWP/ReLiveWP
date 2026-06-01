namespace ReLiveWP.Services.Push.Data;

public class QueuedNotification
{
    public long Id { get; set; }
    public string DeviceId { get; set; }
    public long ChannelId { get; set; }

    public byte[] Payload { get; set; }
    public uint NotificationClass { get; set; }

    public NotificationStatus Status { get; set; }
    public int AttemptCount { get; set; }

    // unix milliseconds, sqlite doesn't like DateTimeOffset comparisons
    public long CreatedAt { get; set; }
    public long? ExpiresAt { get; set; }

    public string LeaseOwner { get; set; }
    public long? LeaseExpiresAt { get; set; }
    public string LastError { get; set; }
}
