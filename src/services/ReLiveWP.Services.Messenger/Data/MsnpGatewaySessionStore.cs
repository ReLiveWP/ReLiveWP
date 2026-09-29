using System.Diagnostics;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger.Msnp;
using StackExchange.Redis;

namespace ReLiveWP.Services.Messenger.Data;

public class MsnpGatewaySessionStore(IConnectionMultiplexer redis, IOptions<MessengerOptions> options) : IMsnpGatewaySessionStore
{
    // dont send too many commands in one go
    private const long MaxDrainCount = 1000;

    private static string SessionKey(string sessionId) => $"msnp:gateway:session:{sessionId}";
    private static string OutboxKey(string sessionId) => $"msnp:gateway:outbox:{sessionId}";
    private static string DoorbellKey(string sessionId) => $"msnp:gateway:doorbell:{sessionId}";
    private static RedisChannel NotifyChannel(string sessionId) =>
        RedisChannel.Literal($"msnp:gateway:notify:{sessionId}");

    private readonly IDatabase db = redis.GetDatabase();
    private readonly ISubscriber subscriber = redis.GetSubscriber();

    private TimeSpan SessionTtl(MsnpGatewaySession session) =>
        options.Value.ResolveSessionTtl(session.State, session.SessionTimeoutSeconds);

    public async Task<MsnpGatewaySession?> FindAsync(string sessionId, CancellationToken ct = default)
    {
        var value = await db.StringGetAsync(SessionKey(sessionId));
        return value.IsNull ? null : JsonSerializer.Deserialize<MsnpGatewaySession>((string)value!);
    }

    public Task TouchAsync(MsnpGatewaySession session, CancellationToken ct = default) =>
        db.KeyExpireAsync(SessionKey(session.SessionId), SessionTtl(session));

    public Task SaveAsync(MsnpGatewaySession session, CancellationToken ct = default) =>
        db.StringSetAsync(SessionKey(session.SessionId), JsonSerializer.Serialize(session), SessionTtl(session));

    public Task DeleteAsync(string sessionId, CancellationToken ct = default) =>
        db.KeyDeleteAsync([SessionKey(sessionId), OutboxKey(sessionId), DoorbellKey(sessionId)]);

    // sessions mostly end by TTL rather than an explicit OUT, so live count has to be sampled
    public static async Task<long> CountSessionsAsync(IConnectionMultiplexer redis, CancellationToken ct = default)
    {
        var server = redis.GetServer(redis.GetEndPoints()[0]);
        long count = 0;
        await foreach (var _ in server.KeysAsync(pattern: SessionKey("*")).WithCancellation(ct))
            count++;
        return count;
    }

    private record OutboxEntry(string Verb, string TrId, string[] Arguments, byte[]? Payload);

    private static RedisValue ToOutboxValue(MsnpCommand command) =>
        JsonSerializer.Serialize(new OutboxEntry(command.Verb, command.TrId, command.Arguments, command.Payload));

    public async Task EnqueueAsync(MsnpGatewaySession session, IEnumerable<MsnpCommand> commands, CancellationToken ct = default)
    {
        var entries = commands.ToArray();
        var values = entries.Select(ToOutboxValue).ToArray();
        if (values.Length == 0)
            return;

        var outboxKey = OutboxKey(session.SessionId);
        var depth = await db.ListRightPushAsync(outboxKey, values);
        MessengerMetrics.RecordOutboxEnqueue(entries, depth);
        await db.KeyExpireAsync(outboxKey, SessionTtl(session));

        await subscriber.PublishAsync(NotifyChannel(session.SessionId), RedisValue.EmptyString);
    }

    private async Task<MsnpGatewayDrain> DrainAsync(string sessionId)
    {
        var commandsDrain = DrainOutboxAsync(sessionId);
        var deliveriesDrain = DrainChatDeliveriesAsync(sessionId);
        var commands = await commandsDrain;
        var deliveries = await deliveriesDrain;

        return commands.Count == 0 && deliveries.Count == 0
            ? MsnpGatewayDrain.Empty
            : new MsnpGatewayDrain(commands, deliveries);
    }

    private async Task<IReadOnlyList<MsnpCommand>> DrainOutboxAsync(string sessionId)
    {
        var values = await db.ListLeftPopAsync(OutboxKey(sessionId), MaxDrainCount);
        if (values is not { Length: > 0 })
            return [];

        var commands = new List<MsnpCommand>(values.Length);
        foreach (var value in values)
        {
            var entry = JsonSerializer.Deserialize<OutboxEntry>((string)value!);
            if (entry is null)
                continue;

            var command = MsnpCommand.Create(entry.Verb, entry.TrId, entry.Arguments);
            if (entry.Payload is not null)
                command = command.WithPayload(entry.Payload);

            commands.Add(command);
        }

        return commands;
    }

    private async Task<IReadOnlyList<ChatDelivery>> DrainChatDeliveriesAsync(string sessionId)
    {
        var values = await db.ListLeftPopAsync(ChatEvents.DeliveryQueueKey(sessionId), MaxDrainCount);
        if (values is not { Length: > 0 })
            return [];

        return values.Select(value => ChatDelivery.Parser.ParseFrom((byte[])value!)).ToList();
    }

    public async Task RequeueAsync(MsnpGatewaySession session, MsnpGatewayDrain drain)
    {
        if (drain.IsEmpty)
            return;

        var ttl = SessionTtl(session);
        var outboxKey = OutboxKey(session.SessionId);
        var chatKey = ChatEvents.DeliveryQueueKey(session.SessionId);

        RedisValue[] commands = [.. drain.Commands.Reverse().Select(ToOutboxValue)];
        RedisValue[] deliveries = [.. drain.Deliveries.Reverse().Select(d => (RedisValue)d.ToByteArray())];

        var transaction = db.CreateTransaction();
        if (commands.Length > 0)
        {
            _ = transaction.ListLeftPushAsync(outboxKey, commands);
            _ = transaction.KeyExpireAsync(outboxKey, ttl);
        }

        if (deliveries.Length > 0)
        {
            _ = transaction.ListLeftPushAsync(chatKey, deliveries);
            _ = transaction.KeyExpireAsync(chatKey, ttl);
        }

        await transaction.ExecuteAsync();
        await subscriber.PublishAsync(NotifyChannel(session.SessionId), RedisValue.EmptyString);
    }

    public Task<long> NotifyAsync(string sessionId) =>
        subscriber.PublishAsync(NotifyChannel(sessionId), RedisValue.EmptyString);

    public async Task<bool> HasPendingAsync(string sessionId)
    {
        var outboxLengthRead = db.ListLengthAsync(OutboxKey(sessionId));
        var chatLengthRead = db.ListLengthAsync(ChatEvents.DeliveryQueueKey(sessionId));
        return await outboxLengthRead + await chatLengthRead > 0;
    }

    public Task<bool> TryClaimDoorbellAsync(string sessionId, TimeSpan window) =>
        db.StringSetAsync(DoorbellKey(sessionId), 1, window, When.NotExists);

    public async Task<MsnpGatewayDrain> WaitAndDrainAsync(string sessionId, TimeSpan timeout, CancellationToken ct = default)
    {
        await db.KeyDeleteAsync(DoorbellKey(sessionId));

        var started = Stopwatch.GetTimestamp();
        var pending = await DrainAsync(sessionId);
        if (!pending.IsEmpty || timeout <= TimeSpan.Zero)
        {
            MessengerMetrics.RecordPollWait(started, "immediate");
            return pending;
        }

        var channel = NotifyChannel(sessionId);
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(RedisChannel _, RedisValue __) => signal.TrySetResult();

        await subscriber.SubscribeAsync(channel, Handler);
        MessengerMetrics.PollsActive.Add(1);

        var outcome = "aborted";
        try
        {
            pending = await DrainAsync(sessionId);
            if (!pending.IsEmpty)
            {
                outcome = "immediate";
                return pending;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await signal.Task.WaitAsync(timeoutCts.Token);
                outcome = "woken";
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Lifespan elapsed with nothing queued - a legitimate empty long-poll result.
                outcome = "timed_out";
            }

            return await DrainAsync(sessionId);
        }
        finally
        {
            MessengerMetrics.PollsActive.Add(-1);
            MessengerMetrics.RecordPollWait(started, outcome);
            await subscriber.UnsubscribeAsync(channel, Handler);
        }
    }
}
