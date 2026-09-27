using ReLiveWP.Services.Push.Nsp;

namespace ReLiveWP.Services.Push.Services;

public class PresenceHeartbeatService(
    PushPresence presence,
    PresenceDirectory directory,
    PushInstance instance,
    ILogger<PresenceHeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                foreach (var deviceId in presence.LocalDevices)
                {
                    try
                    {
                        await directory.SetAsync(deviceId, instance.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "presence heartbeat failed for {DeviceId}", deviceId);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
