namespace ReLiveWP.Services.Push.Data;

public class PushChannel
{
    public long ChannelId { get; set; }
    public string Token { get; set; }
    public string DeviceId { get; set; }

    public string ChannelName { get; set; }
    public string ServiceName { get; set; }
    public string Version { get; set; }
    public string Identifier { get; set; }

    public long CreatedAt { get; set; }
}
