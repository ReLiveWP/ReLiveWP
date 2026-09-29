using ReLiveWP.Services.Messenger.Data;

namespace ReLiveWP.Services.Messenger;

public class MessengerOptions
{
    public const string SectionName = "Messenger";

    public TimeSpan MaxSessionTimeout { get; set; } = TimeSpan.FromDays(3);
    public TimeSpan PreAuthSessionTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan DoorbellRepeatAfter { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan DeviceRetention { get; set; } = TimeSpan.FromDays(30);
    public HashSet<string> PushHosts { get; set; } = new(["push.relivewp.net", "push.int.relivewp.net"], StringComparer.OrdinalIgnoreCase);
    public int MaxTextSdgBytes { get; set; } = 8 * 1024;
    public int MaxDataSdgBytes { get; set; } = 64 * 1024;
    public int MaxCommandsPerRequest { get; set; } = 32;

    public TimeSpan ResolveSessionTtl(MsnpGatewaySessionState state, int? requestedSeconds) =>
        state == MsnpGatewaySessionState.AwaitingSsoTicket
            ? PreAuthSessionTimeout
            : ResolveSessionTtl(requestedSeconds);

    public TimeSpan ResolveSessionTtl(int? requestedSeconds)
    {
        if (requestedSeconds is not > 0)
            return MaxSessionTimeout;

        var requested = TimeSpan.FromSeconds(requestedSeconds.Value);
        return requested < MaxSessionTimeout ? requested : MaxSessionTimeout;
    }
}
