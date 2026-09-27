namespace ReLiveWP.Services.Push.Data;

public class QueuedNotification
{
    public string StreamId { get; set; }
    public string DeviceId { get; set; }
    public long ChannelId { get; set; }

    public byte[] Payload { get; set; }
    public uint NotificationClass { get; set; }

    public long? ExpiresAt { get; set; }
}
