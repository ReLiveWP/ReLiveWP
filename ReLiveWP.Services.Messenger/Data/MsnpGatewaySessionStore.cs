using System.Text.Json;
using ReLiveWP.Services.Messenger.Msnp;
using StackExchange.Redis;

namespace ReLiveWP.Services.Messenger.Data;

public class MsnpGatewaySessionStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(3);

    // dont send too many commands in one go
    private const long MaxDrainCount = 1000;

    private static string SessionKey(string sessionId) => $"msnp:gateway:session:{sessionId}";
    private static string OutboxKey(string sessionId) => $"msnp:gateway:outbox:{sessionId}";
    private static RedisChannel NotifyChannel(string sessionId) =>
        RedisChannel.Literal($"msnp:gateway:notify:{sessionId}");

    private readonly IDatabase db = redis.GetDatabase();
    private readonly ISubscriber subscriber = redis.GetSubscriber();

    public Task CreateAsync(MsnpGatewaySession session, CancellationToken ct = default)
    {
        var ttl = session.SessionTimeoutSeconds is > 0
            ? TimeSpan.FromSeconds(session.SessionTimeoutSeconds.Value)
            : DefaultTtl;

        return db.StringSetAsync(SessionKey(session.SessionId), JsonSerializer.Serialize(session), ttl);
    }

    public async Task<MsnpGatewaySession?> FindAsync(string sessionId, CancellationToken ct = default)
    {
        var value = await db.StringGetAsync(SessionKey(sessionId));
        return value.IsNull ? null : JsonSerializer.Deserialize<MsnpGatewaySession>((string)value!);
    }

    public Task TouchAsync(string sessionId, CancellationToken ct = default) =>
        db.KeyExpireAsync(SessionKey(sessionId), DefaultTtl);

    public Task SaveAsync(MsnpGatewaySession session, CancellationToken ct = default) =>
        db.StringSetAsync(SessionKey(session.SessionId), JsonSerializer.Serialize(session), keepTtl: true);

    public Task DeleteAsync(string sessionId, CancellationToken ct = default) =>
        db.KeyDeleteAsync([SessionKey(sessionId), OutboxKey(sessionId)]);

    private record OutboxEntry(string Verb, string TrId, string[] Arguments, string? Payload);

    public async Task EnqueueAsync(string sessionId, IEnumerable<MsnpCommand> commands, CancellationToken ct = default)
    {
        var values = commands
            .Select(c => (RedisValue)JsonSerializer.Serialize(new OutboxEntry(c.Verb, c.TrId, c.Arguments, c.Payload)))
            .ToArray();
        if (values.Length == 0)
            return;

        await db.ListRightPushAsync(OutboxKey(sessionId), values);
        await db.KeyExpireAsync(OutboxKey(sessionId), DefaultTtl);

        await subscriber.PublishAsync(NotifyChannel(sessionId), RedisValue.EmptyString);
    }

    public async Task<IReadOnlyList<MsnpCommand>> DrainAsync(string sessionId, CancellationToken ct = default)
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

    public async Task<IReadOnlyList<MsnpCommand>> WaitAndDrainAsync(string sessionId, TimeSpan timeout, CancellationToken ct = default)
    {
        var pending = await DrainAsync(sessionId, ct);
        if (pending.Count > 0 || timeout <= TimeSpan.Zero)
            return pending;

        var channel = NotifyChannel(sessionId);
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(RedisChannel _, RedisValue __) => signal.TrySetResult();

        await subscriber.SubscribeAsync(channel, Handler);
        try
        {
            pending = await DrainAsync(sessionId, ct);
            if (pending.Count > 0)
                return pending;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await signal.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Lifespan elapsed with nothing queued - a legitimate empty long-poll result.
            }

            return await DrainAsync(sessionId, ct);
        }
        finally
        {
            await subscriber.UnsubscribeAsync(channel, Handler);
        }
    }
}
