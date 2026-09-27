using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Nsp;
using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Services;

public class NotificationCleanupService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    PushInstance instance,
    ILogger<NotificationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    // one sweep per tick across all replicas; the lock outlives a crashed sweeper by expiring
    private const string SweepLockKey = "push:sweep:lock";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    if (!await redis.GetDatabase().StringSetAsync(SweepLockKey, instance.Id, Interval, When.NotExists))
                        continue;

                    using var scope = scopeFactory.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<NotificationQueue>();

                    var expired = await queue.ExpireAsync(ct);
                    if (expired > 0)
                    {
                        logger.LogInformation("Expired {Expired} notification(s)", expired);
                        PushMetrics.NotificationsExpired.Add(expired);
                    }
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
