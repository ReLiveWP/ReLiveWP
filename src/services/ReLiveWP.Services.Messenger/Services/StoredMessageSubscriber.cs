using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Services.Messenger.Services;

public class StoredMessageSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<StoredMessageSubscriber> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        redis.ReadEventsAsync<ChatMessageStoredEvent>(
            ChatEvents.MessageStored, evt => _ = RingDevicesAsync(evt.RecipientUserId, stoppingToken), logger, stoppingToken);

    private async Task RingDevicesAsync(string userId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMsnpDoorbell>().RingDevicesAsync(userId, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "could not ring the devices of {User} for a stored message", userId);
        }
    }
}
