namespace ReLiveWP.Backend.Mailbox.Services;

public class SyncStateRepairBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncStateRepairBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // cleaning up orphaned or otherwise invalid sync states at startup

            using var scope = scopeFactory.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<SyncStateRepairService>();

            var states = await reconciler.SweepAsync(null, stoppingToken);

            if (states > 0)
                logger.LogInformation(
                    "sync-state sweep: deleted {States} orphaned SyncState rows", states);

            foreach (var device in await reconciler.UnknownDevicesAsync(null, stoppingToken))
                logger.LogWarning(
                    "sync state for {User} on {Device} has no DeviceInfo row, keeping {Rows} rows. "
                    + "a device id that never provisioned usually means one was parsed wrong.",
                    device.UserId, device.DeviceId, device.Rows);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "sync-state orphan sweep skipped; will retry next start");
        }
    }
}
