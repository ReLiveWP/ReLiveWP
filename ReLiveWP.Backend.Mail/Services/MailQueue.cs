using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mail.Services;

public sealed record QueuedMail(string StreamId, MailEnvelope Envelope, byte[] Message);

public interface IMailQueue
{
    Task EnqueueAsync(MailEnvelope envelope, byte[] message, CancellationToken ct);

    Task<IReadOnlyList<QueuedMail>> DequeueAsync(int count, CancellationToken ct);

    Task CompleteAsync(QueuedMail item, CancellationToken ct);
}

public class RedisMailQueue(IConnectionMultiplexer redis) : IMailQueue
{
    private const string Key = "mail:outbound";
    private const string Group = "mail";
    private const int MaxLen = 10000;

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

        // anything already handed to this consumer and never acked comes back first, so a crash
        // mid-delivery retries instead of dropping the message
        var pending = await db.StreamReadGroupAsync(Key, Group, Consumer, StreamPosition.Beginning, count);
        var entries = pending.Length > 0
            ? pending
            : await db.StreamReadGroupAsync(Key, Group, Consumer, StreamPosition.NewMessages, count);

        return [.. entries.Select(Parse).OfType<QueuedMail>()];
    }

    public async Task CompleteAsync(QueuedMail item, CancellationToken ct)
    {
        await db.StreamAcknowledgeAsync(Key, Group, item.StreamId);
        await db.StreamDeleteAsync(Key, [(RedisValue)item.StreamId]);
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

    private static QueuedMail? Parse(StreamEntry entry)
    {
        var envelope = Field(entry, "e");
        var message = Field(entry, "m");
        if (!envelope.HasValue || !message.HasValue)
            return null;

        var parsed = JsonSerializer.Deserialize<MailEnvelope>((string)envelope!);
        return parsed is null ? null : new QueuedMail(entry.Id!, parsed, (byte[])message!);
    }

    private static RedisValue Field(StreamEntry entry, string name)
    {
        foreach (var pair in entry.Values)
            if (pair.Name == name)
                return pair.Value;
        return RedisValue.Null;
    }
}
