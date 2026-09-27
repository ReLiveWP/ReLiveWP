using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Nsp;

public record RoutedNotification(string DeviceId, uint ChannelId, uint Class, byte[] Payload);

public class PushRouter(IConnectionMultiplexer redis)
{
    public static RedisChannel Channel(string instanceId) => RedisChannel.Literal($"push:route:{instanceId}");

    public Task PublishAsync(string instanceId, RoutedNotification notification) =>
        redis.GetSubscriber().PublishAsync(Channel(instanceId),
            JsonSerializer.SerializeToUtf8Bytes(notification));
}
