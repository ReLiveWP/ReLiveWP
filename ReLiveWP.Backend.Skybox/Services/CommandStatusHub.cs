using System.Runtime.CompilerServices;
using System.Text.Json;
using ReLiveWP.Backend.Skybox.Commands;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Skybox.Services;

public record CommandStatusEvent(
    uint RequestId,
    DeviceCommandAction Action,
    uint Result,
    bool Final,
    string? Data,
    DateTimeOffset Reported,
    double? Lat,
    double? Long,
    double? Accuracy);

public class CommandStatusHub(IConnectionMultiplexer redis)
{
    private static RedisChannel Channel(string deviceId) => RedisChannel.Literal($"sky:cmd-status:{deviceId}");

    public Task PublishAsync(string deviceId, CommandStatusEvent evt) =>
        redis.GetSubscriber().PublishAsync(Channel(deviceId), JsonSerializer.SerializeToUtf8Bytes(evt));

    public async IAsyncEnumerable<CommandStatusEvent> StreamAsync(
        string deviceId, [EnumeratorCancellation] CancellationToken ct)
    {
        var queue = await redis.GetSubscriber().SubscribeAsync(Channel(deviceId));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var message = await queue.ReadAsync(ct);
                var evt = JsonSerializer.Deserialize<CommandStatusEvent>((byte[])message.Message!);
                if (evt != null)
                    yield return evt;
            }
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }
}
