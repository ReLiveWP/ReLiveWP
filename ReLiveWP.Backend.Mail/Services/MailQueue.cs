using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mail.Services;

public sealed record QueuedMail(string StreamId, MailEnvelope Envelope, byte[] Message, int Attempts);

public interface IMailQueue
{
    Task EnqueueAsync(MailEnvelope envelope, byte[] message, CancellationToken ct);

    Task<IReadOnlyList<QueuedMail>> DequeueAsync(int count, CancellationToken ct);

    Task CompleteAsync(QueuedMail item, CancellationToken ct);

    Task DeadLetterAsync(QueuedMail item, string reason, CancellationToken ct);
}

public class RedisMailQueue(IConnectionMultiplexer redis) : IMailQueue
{
    private const string Key = "mail:outbound";
    private const string DeadLetterKey = "mail:outbound:dead";
    private const string Group = "mail";
    private const int MaxLen = 10000;

    // a failed delivery sits pending this long before anyone retries it
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

    private static readonly string Consumer = Environment.MachineName;

    private readonly IDatabase db = redis.GetDatabase();

    public async Task EnqueueAsync(MailEnvelope envelope, byte[] message, CancellationToken ct)
    {
        await EnsureGroupAsync();
        await db.StreamAddAsync(
            Key,
            [new("e", JsonSerializer.Serialize(envelope)), new("m", message)],
            maxLength: MaxLen,
            useApproximateMaxLength: true);
    }

    public async Task<IReadOnlyList<QueuedMail>> DequeueAsync(int count, CancellationToken ct)
    {
        await EnsureGroupAsync();

        // retries first, claimed from whichever consumer had them, so a crashed replica's
        // in-flight mail comes back too. then fresh messages, so one stuck retry never blocks the queue
        var claimed = await db.StreamAutoClaimAsync(Key, Group, Consumer, (long)RetryDelay.TotalMilliseconds, "0-0", count);
        var retries = claimed.IsNull ? [] : claimed.ClaimedEntries;
        var attempts = await AttemptsAsync(retries);

        var fresh = retries.Length < count
            ? await db.StreamReadGroupAsync(Key, Group, Consumer, StreamPosition.NewMessages, count - retries.Length)
            : [];

        var batch = new List<QueuedMail>(retries.Length + fresh.Length);
        foreach (var entry in retries)
            if (Parse(entry, attempts.GetValueOrDefault(entry.Id, 1)) is { } item)
                batch.Add(item);
        foreach (var entry in fresh)
            if (Parse(entry, 1) is { } item)
                batch.Add(item);

        return batch;
    }

    public async Task CompleteAsync(QueuedMail item, CancellationToken ct)
    {
        await db.StreamAcknowledgeAsync(Key, Group, item.StreamId);
        await db.StreamDeleteAsync(Key, [(RedisValue)item.StreamId]);
    }

    public async Task DeadLetterAsync(QueuedMail item, string reason, CancellationToken ct)
    {
        await db.StreamAddAsync(
            DeadLetterKey,
            [
                new("e", JsonSerializer.Serialize(item.Envelope)),
                new("m", item.Message),
                new("reason", reason),
                new("attempts", item.Attempts),
                new("failed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            ],
            maxLength: MaxLen,
            useApproximateMaxLength: true);

        await CompleteAsync(item, ct);
    }

    private async Task<Dictionary<RedisValue, int>> AttemptsAsync(StreamEntry[] entries)
    {
        if (entries.Length == 0)
            return [];

        var pending = await db.StreamPendingMessagesAsync(Key, Group, entries.Length, Consumer, entries[0].Id, entries[^1].Id);
        return pending.ToDictionary(p => p.MessageId, p => p.DeliveryCount);
    }

    private async Task EnsureGroupAsync()
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(Key, Group, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // group already exists
        }
    }

    private static QueuedMail? Parse(StreamEntry entry, int attempts)
    {
        var envelope = Field(entry, "e");
        var message = Field(entry, "m");
        if (!envelope.HasValue || !message.HasValue)
            return null;

        var parsed = JsonSerializer.Deserialize<MailEnvelope>((string)envelope!);
        return parsed is null ? null : new QueuedMail(entry.Id!, parsed, (byte[])message!, attempts);
    }

    private static RedisValue Field(StreamEntry entry, string name)
    {
        foreach (var pair in entry.Values)
            if (pair.Name == name)
                return pair.Value;
        return RedisValue.Null;
    }
}
