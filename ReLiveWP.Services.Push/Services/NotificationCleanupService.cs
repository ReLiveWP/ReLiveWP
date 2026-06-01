using ReLiveWP.Services.Push.Data;

namespace ReLiveWP.Services.Push.Services;

public class NotificationCleanupService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    // notificationqueue is scoped, so spin a scope per sweep
                    using var scope = scopeFactory.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<NotificationQueue>();

                    var reclaimed = await queue.ReclaimExpiredAsync(ct);
                    var expired = await queue.ExpireAsync(ct);
                    if (reclaimed > 0 || expired > 0)
                        logger.LogInformation("Reclaimed {Reclaimed} lease(s), expired {Expired}", reclaimed, expired);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Sweep failed");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
