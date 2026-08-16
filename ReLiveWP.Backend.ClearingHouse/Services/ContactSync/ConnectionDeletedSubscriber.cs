using System.Text.Json;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public sealed class ConnectionDeletedSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<ConnectionDeletedSubscriber> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().SubscribeAsync(
            AccountEvents.ConnectionDeleted, (channel, value) => { _ = HandleAsync(value); });

        logger.LogInformation("listening for connection.deleted");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(AccountEvents.ConnectionDeleted);
    }

    private async Task HandleAsync(RedisValue value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<ConnectionDeletedEvent>((byte[])value!);
            if (evt is null) return;

            using var scope = scopeFactory.CreateScope();
            var detach = scope.ServiceProvider.GetRequiredService<ContactSourceDetach>();

            var sources = await detach.ForConnectionAsync(evt.UserId, evt.ConnectionId);
            if (sources.Count == 0) return;

            if (evt.DeleteData)
            {
                var removed = await detach.DeleteAndDetachAsync(sources);
                logger.LogInformation("{Connection} was unlinked and removed {Count} contact(s)",
                    evt.ConnectionId, removed);

                return;
            }

            await detach.DetachAsync(sources);
            logger.LogInformation("{Connection} was unlinked; its contacts stay and are now the user's own",
                evt.ConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed handling connection.deleted");
        }
    }
}
