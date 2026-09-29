using Google.Protobuf;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc.Chat;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Chat.Data;

public class RedisStoredMessageStore(IConnectionMultiplexer redis, IOptions<ChatOptions> options, TimeProvider time) : IStoredMessageStore
{
    private const string DueKey = "chat:stored:due";

    private const string StoreScript = """
        if redis.call('LLEN', KEYS[2]) >= tonumber(ARGV[4]) then return 0 end
        local fromSender = 0
        for _, entry in ipairs(redis.call('LRANGE', KEYS[2], 0, -1)) do
            if string.match(entry, ' (.+)$') == ARGV[3] then fromSender = fromSender + 1 end
        end
        if fromSender >= tonumber(ARGV[5]) then return 0 end
        redis.call('RPUSH', KEYS[1], ARGV[1])
        redis.call('RPUSH', KEYS[2], ARGV[2])
        redis.call('PEXPIRE', KEYS[1], ARGV[6])
        redis.call('PEXPIRE', KEYS[2], ARGV[6])
        redis.call('ZADD', KEYS[3], 'NX', ARGV[7], ARGV[8])
        return 1
        """;

    private const string DeliverScript = """
        local items = redis.call('LRANGE', KEYS[1], 0, -1)
        if #items == 0 then
            redis.call('DEL', KEYS[1], KEYS[2])
            redis.call('ZREM', KEYS[3], ARGV[1])
            return 0
        end
        local cap = tonumber(ARGV[2])
        local targets = #KEYS - 4
        local delivered = 0
        for t = 1, targets do
            local queue = KEYS[4 + t]
            local endpointId = ARGV[3 + t]
            local timeout = ARGV[3 + targets + t]
            if redis.call('LLEN', queue) + #items > cap then
                redis.call('ZADD', KEYS[4], 'NX', ARGV[3], endpointId)
            else
                for first = 1, #items, 500 do
                    redis.call('RPUSH', queue, unpack(items, first, math.min(first + 499, #items)))
                end
                redis.call('PEXPIRE', queue, timeout)
                delivered = delivered + 1
            end
        end
        if delivered == 0 then return 0 end
        redis.call('DEL', KEYS[1], KEYS[2])
        redis.call('ZREM', KEYS[3], ARGV[1])
        return #items
        """;

    private const string DropExpiredScript = """
        local index = redis.call('LRANGE', KEYS[2], 0, -1)
        local expired = 0
        for _, entry in ipairs(index) do
            if tonumber(string.match(entry, '^(%d+)')) > tonumber(ARGV[1]) then break end
            expired = expired + 1
        end
        if expired == #index then
            redis.call('DEL', KEYS[1], KEYS[2])
            return expired
        end
        if expired > 0 then
            redis.call('LTRIM', KEYS[1], expired, -1)
            redis.call('LTRIM', KEYS[2], expired, -1)
        end
        local oldest = tonumber(string.match(index[expired + 1], '^(%d+)'))
        redis.call('ZADD', KEYS[3], string.format('%d', oldest + tonumber(ARGV[2])), ARGV[3])
        return expired
        """;

    private readonly IDatabase db = redis.GetDatabase();

    private static RedisKey MessagesKey(string userId) => $"chat:stored:{userId}";
    private static RedisKey IndexKey(string userId) => $"chat:stored:{userId}:index";

    private TimeSpan Retention => options.Value.StoredMessageRetention;

    public async Task<bool> TryStoreAsync(string recipientId, string senderId, ChatDelivery delivery)
    {
        var arrival = delivery.MessageReceived.OriginalArrivalUnixMs;
        var retentionMs = (long)Retention.TotalMilliseconds;

        var result = await db.ScriptEvaluateAsync(
            StoreScript,
            [MessagesKey(recipientId), IndexKey(recipientId), DueKey],
            [
                delivery.ToByteArray(),
                $"{arrival} {senderId}",
                senderId,
                options.Value.MaxStoredPerRecipient,
                options.Value.MaxStoredPerPair,
                retentionMs,
                arrival + retentionMs,
                recipientId,
            ]);

        if ((int)result != 1)
            return false;

        await redis.PublishMessageStoredAsync(new ChatMessageStoredEvent(recipientId));
        return true;
    }

    public async Task<int> DeliverStoredAsync(string recipientId, IReadOnlyList<ChatEndpoint> targets)
    {
        if (targets.Count == 0)
            return 0;

        RedisKey[] keys =
        [
            MessagesKey(recipientId),
            IndexKey(recipientId),
            DueKey,
            RedisDeliveryQueue.OverflowKey,
            .. targets.Select(t => ChatEvents.DeliveryQueueKey(t.EndpointId)),
        ];
        RedisValue[] values =
        [
            recipientId,
            options.Value.MaxQueuedPerEndpoint,
            time.GetUtcNow().ToUnixTimeMilliseconds(),
            .. targets.Select(t => (RedisValue)t.EndpointId),
            .. targets.Select(t => (RedisValue)(long)t.SessionTimeout.TotalMilliseconds),
        ];

        var delivered = (int)await db.ScriptEvaluateAsync(DeliverScript, keys, values);
        if (delivered == 0)
            return 0;

        foreach (var target in targets)
            await redis.PublishDeliveryQueuedAsync(new ChatDeliveryQueuedEvent(target.EndpointId));

        return delivered;
    }

    public async Task<int> DropExpiredAsync(DateTimeOffset now)
    {
        var claimed = await RedisChatStateStore.ClaimDueAsync(db, DueKey, now);

        var dropped = 0;
        foreach (var recipientId in claimed)
        {
            var cutoff = now - Retention;
            var result = await db.ScriptEvaluateAsync(
                DropExpiredScript,
                [MessagesKey(recipientId), IndexKey(recipientId), DueKey],
                [cutoff.ToUnixTimeMilliseconds(), (long)Retention.TotalMilliseconds, recipientId]);

            dropped += (int)result;
        }

        return dropped;
    }
}
