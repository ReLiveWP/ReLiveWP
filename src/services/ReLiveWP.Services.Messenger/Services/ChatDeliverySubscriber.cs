using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Services.Messenger.Services;

public class ChatDeliverySubscriber(
    IConnectionMultiplexer redis,
    IMsnpSessionWaker waker,
    ILogger<ChatDeliverySubscriber> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        redis.ReadEventsAsync<ChatDeliveryQueuedEvent>(
            ChatEvents.DeliveryQueued, evt => waker.WakeSessionLater(evt.EndpointId), logger, stoppingToken);
}
