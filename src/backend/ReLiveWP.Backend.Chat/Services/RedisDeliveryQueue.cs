using System.Diagnostics;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc.Chat;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Chat.Services;

public class RedisDeliveryQueue(IConnectionMultiplexer redis, IOptions<ChatOptions> options, TimeProvider time) : IDeliveryQueue
{
    internal const string OverflowKey = "chat:overflow";

    private const string EnqueueScript = """
        if redis.call('LLEN', KEYS[1]) >= tonumber(ARGV[2]) then
            redis.call('ZADD', KEYS[2], 'NX', ARGV[4], ARGV[5])
            return 0
        end
        redis.call('RPUSH', KEYS[1], ARGV[1])
        redis.call('PEXPIRE', KEYS[1], ARGV[3])
        return 1
        """;

    private readonly IDatabase db = redis.GetDatabase();

    public async Task<bool> EnqueueAsync(ChatEndpoint target, ChatDelivery delivery)
    {
        var now = time.GetUtcNow();
        delivery.QueuedAtUnixMs = now.ToUnixTimeMilliseconds();
        delivery.TraceParent = Activity.Current?.Id ?? "";

        var result = await db.ScriptEvaluateAsync(
            EnqueueScript,
            [ChatEvents.DeliveryQueueKey(target.EndpointId), OverflowKey],
            [
                delivery.ToByteArray(),
                options.Value.MaxQueuedPerEndpoint,
                (long)target.SessionTimeout.TotalMilliseconds,
                now.ToUnixTimeMilliseconds(),
                target.EndpointId,
            ]);

        if ((int)result != 1)
            return false;

        await redis.PublishDeliveryQueuedAsync(new ChatDeliveryQueuedEvent(target.EndpointId));
        ChatMetrics.RecordDeliveryQueued(delivery.KindCase);
        return true;
    }

    public Task ClearAsync(string endpointId) => db.KeyDeleteAsync(ChatEvents.DeliveryQueueKey(endpointId));

    public async Task<int> MoveMessagesAsync(string fromEndpointId, ChatEndpoint target)
    {
        var fromKey = ChatEvents.DeliveryQueueKey(fromEndpointId);
        var take = db.CreateTransaction();
        var pendingRead = take.ListRangeAsync(fromKey);
        _ = take.KeyDeleteAsync(fromKey);
        await take.ExecuteAsync();
        var pending = await pendingRead;

        RedisValue[] messages = [.. pending.Where(IsMessageDelivery)];
        if (messages.Length == 0)
            return 0;

        var queueKey = ChatEvents.DeliveryQueueKey(target.EndpointId);
        var transaction = db.CreateTransaction();
        _ = transaction.ListRightPushAsync(queueKey, messages);
        _ = transaction.KeyExpireAsync(queueKey, target.SessionTimeout);
        await transaction.ExecuteAsync();

        await redis.PublishDeliveryQueuedAsync(new ChatDeliveryQueuedEvent(target.EndpointId));
        return messages.Length;
    }

    private static bool IsMessageDelivery(RedisValue value)
    {
        var delivery = ChatDelivery.Parser.ParseFrom((byte[])value!);
        return delivery.KindCase == ChatDelivery.KindOneofCase.MessageReceived;
    }

    public Task<IReadOnlyList<string>> ClaimOverflowingAsync(DateTimeOffset now) =>
        RedisChatStateStore.ClaimDueAsync(db, OverflowKey, now);

    public Task ScheduleOverflowEvictionAsync(string endpointId, DateTimeOffset dueAt) =>
        db.SortedSetAddAsync(OverflowKey, endpointId, dueAt.ToUnixTimeMilliseconds(), SortedSetWhen.NotExists);
}
