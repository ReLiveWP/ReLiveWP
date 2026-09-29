using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ReLiveWP.ServiceDefaults.Events;

public static class RedisEventReader
{
    public static async Task ReadEventsAsync<TEvent>(
        this IConnectionMultiplexer redis, RedisChannel channel, Action<TEvent> handle, ILogger logger, CancellationToken ct)
    {
        var queue = await redis.GetSubscriber().SubscribeAsync(channel);
        using var unsubscribe = ct.Register(() => queue.Unsubscribe());

        while (!ct.IsCancellationRequested)
        {
            ChannelMessage message;
            try
            {
                message = await queue.ReadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (TryReadEvent<TEvent>(message, logger) is { } evt)
                handle(evt);
        }
    }

    private static TEvent? TryReadEvent<TEvent>(ChannelMessage message, ILogger logger)
    {
        try
        {
            return JsonSerializer.Deserialize<TEvent>((byte[])message.Message!);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "ignoring an unreadable {Event} on {Channel}", typeof(TEvent).Name, message.Channel);
            return default;
        }
    }
}
