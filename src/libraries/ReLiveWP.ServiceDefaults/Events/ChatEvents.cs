using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.ServiceDefaults.Events;

// broadcast by Backend.Chat after it queues a delivery for an endpoint, so whichever front end
// owns that endpoint can wake its poll or ring the device. The queue itself is a list of
// protobuf ChatDelivery messages at DeliveryQueueKey.
public record ChatDeliveryQueuedEvent(string EndpointId);

public record ChatMessageStoredEvent(string RecipientUserId);

public static class ChatEvents
{
    public static readonly RedisChannel DeliveryQueued = RedisChannel.Literal("chat.delivery-queued");
    public static readonly RedisChannel MessageStored = RedisChannel.Literal("chat.message-stored");

    public static RedisKey DeliveryQueueKey(string endpointId) => $"chat:ep:{endpointId}";

    public static Task PublishDeliveryQueuedAsync(this IConnectionMultiplexer redis, ChatDeliveryQueuedEvent evt) =>
        redis.GetSubscriber().PublishAsync(DeliveryQueued, JsonSerializer.SerializeToUtf8Bytes(evt));

    public static Task PublishMessageStoredAsync(this IConnectionMultiplexer redis, ChatMessageStoredEvent evt) =>
        redis.GetSubscriber().PublishAsync(MessageStored, JsonSerializer.SerializeToUtf8Bytes(evt));
}
