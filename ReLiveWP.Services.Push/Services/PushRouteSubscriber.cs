using System.Text.Json;
using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Nsp;
using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Services;
public class PushRouteSubscriber(
    IConnectionMultiplexer redis,
    PushPresence presence,
    PushInstance instance,
    IServiceScopeFactory scopeFactory,
    ILogger<PushRouteSubscriber> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var sub = redis.GetSubscriber();
        await sub.SubscribeAsync(PushRouter.Channel(instance.Id), (channel, value) => { _ = HandleAsync(value); });
        logger.LogInformation("Push instance {InstanceId} listening for routed notifications", instance.Id);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(PushRouter.Channel(instance.Id));
    }

    private async Task HandleAsync(RedisValue value)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<RoutedNotification>((byte[])value);

            // device on this instance right now? straight down the socket
            var notificationClass = (NspNotificationClass)msg.Class;
            if (presence.TryGet(msg.DeviceId, out var session)
                && session.TrySend(msg.ChannelId, notificationClass, msg.Payload))
            {
                logger.LogDebug("routed {Class} to {DeviceId} channel {Id}",
                    notificationClass, msg.DeviceId, msg.ChannelId);
                PushMetrics.RecordDelivery("delivered_routed", notificationClass);
                return;
            }

            // otherwise, queue it for delivery when the device reconnects to any instance
            using var scope = scopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<NotificationQueue>();
            await queue.EnqueueAsync(msg.DeviceId, msg.ChannelId, msg.Payload, msg.Class);
            logger.LogDebug("routed miss, queued for {DeviceId} channel {Id}", msg.DeviceId, msg.ChannelId);
            PushMetrics.RecordDelivery("routed_miss", notificationClass);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed handling routed notification");
        }
    }
}
