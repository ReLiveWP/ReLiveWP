using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class TransientConnectionSweeper(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<TransientConnectionSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "transient connection sweep failed; will retry next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedServicesDbContext>();

        var cutoff = DateTimeOffset.UtcNow - MaxAge;

        var stale = await db.ConnectedServices
            .Where(c => (c.Flags & LiveConnectedServiceFlags.Transient) != 0 && c.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        db.ConnectedServices.RemoveRange(stale);
        await db.SaveChangesAsync(ct);

        foreach (var connection in stale)
        {
            var deleted = new ConnectionDeletedEvent(connection.UserId.ToString(), connection.Id.ToString(), connection.Service, DeleteData: false);
            await redis.PublishConnectionDeletedAsync(deleted);
        }

        logger.LogInformation("expired {Count} abandoned transient connection(s)", stale.Count);
    }
}
