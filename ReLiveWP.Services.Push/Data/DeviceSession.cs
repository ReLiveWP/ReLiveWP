namespace ReLiveWP.Services.Push.Data;

public class DeviceSession
{
    public string Token { get; set; }
    public string DeviceId { get; set; }
    public long CreatedAt { get; set; }
    public long LastSeenAt { get; set; }
}
