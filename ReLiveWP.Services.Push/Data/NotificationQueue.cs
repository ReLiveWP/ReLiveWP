using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Data;

public class NotificationQueue(IConnectionMultiplexer redis)
{
    private const string Group = "push";
    private const string Consumer = "push";
    private const int MaxLen = 1000; // bound a device's backlog; oldest entries roll off

    private static string Key(string deviceId) => $"push:queue:{deviceId}";

    private readonly IDatabase db = redis.GetDatabase();

    public async Task EnqueueAsync(
        string deviceId,
        long channelId,
        byte[] payload,
        uint notificationClass,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default)
    {
        var key = Key(deviceId);
        await EnsureGroupAsync(key);

        var fields = new List<NameValueEntry>
        {
            new("d", deviceId),
            new("c", channelId),
            new("n", notificationClass),
            new("p", payload),
        };
        if (expiresAt != null)
            fields.Add(new("e", expiresAt.Value.ToUnixTimeMilliseconds()));

        await db.StreamAddAsync(key, [.. fields], maxLength: MaxLen, useApproximateMaxLength: true);
    }

    public async Task<IReadOnlyList<QueuedNotification>> ClaimForDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var key = Key(deviceId);
        await EnsureGroupAsync(key);

        await db.StreamReadGroupAsync(key, Group, Consumer, StreamPosition.NewMessages, count: MaxLen);
        var entries = await db.StreamReadGroupAsync(key, Group, Consumer, StreamPosition.Beginning, count: MaxLen);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new List<QueuedNotification>();
        foreach (var entry in entries)
        {
            var item = Parse(deviceId, entry);
            if (item.ExpiresAt != null && item.ExpiresAt < now)
            {
                await db.StreamAcknowledgeAsync(key, Group, entry.Id);
                await db.StreamDeleteAsync(key, [entry.Id]);
                continue;
            }
            result.Add(item);
        }
        return result;
    }

    public async Task CompleteAsync(QueuedNotification item, CancellationToken ct = default)
    {
        var key = Key(item.DeviceId);
        await db.StreamAcknowledgeAsync(key, Group, item.StreamId);
        await db.StreamDeleteAsync(key, [(RedisValue)item.StreamId]);
    }

    public Task FailAsync(QueuedNotification item, string error, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task<int> ExpireAsync(CancellationToken ct = default)
    {
        var server = redis.GetServer(redis.GetEndPoints()[0]);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var removed = 0;

        await foreach (var key in server.KeysAsync(pattern: "push:queue:*").WithCancellation(ct))
        {
            foreach (var entry in await db.StreamRangeAsync(key))
            {
                var e = Field(entry, "e");
                if (e.HasValue && (long)e < now)
                {
                    await db.StreamAcknowledgeAsync(key, Group, entry.Id);
                    await db.StreamDeleteAsync(key, [entry.Id]);
                    removed++;
                }
            }
        }
        return removed;
    }

    private async Task EnsureGroupAsync(RedisKey key)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(key, Group, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // group already exists
        }
    }

    private static QueuedNotification Parse(string deviceId, StreamEntry entry)
    {
        var expires = Field(entry, "e");
        return new QueuedNotification
        {
            StreamId = entry.Id,
            DeviceId = deviceId,
            ChannelId = (long)Field(entry, "c"),
            NotificationClass = (uint)(long)Field(entry, "n"),
            Payload = (byte[])Field(entry, "p"),
            ExpiresAt = expires.HasValue ? (long)expires : null,
        };
    }

    private static RedisValue Field(StreamEntry entry, string name)
    {
        foreach (var pair in entry.Values)
            if (pair.Name == name)
                return pair.Value;
        return RedisValue.Null;
    }
}
