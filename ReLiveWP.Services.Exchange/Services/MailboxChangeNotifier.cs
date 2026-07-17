using System.Collections.Concurrent;
using System.Text.Json;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Services.Exchange.Services;

public sealed class MailboxChangeNotifier(
    IConnectionMultiplexer redis,
    ILogger<MailboxChangeNotifier> logger) : IHostedService
{
    private sealed record Waiter(IReadOnlySet<string> Collections, TaskCompletionSource Signal);

    // userId -> (waiterId -> Waiter)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Waiter>> _waiters = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().SubscribeAsync(MailboxEvents.Changed, (_, value) => Handle(value));
        logger.LogInformation("listening for mailbox.changed");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(MailboxEvents.Changed);
    }

    private void Handle(RedisValue value)
    {
        MailboxChangedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<MailboxChangedEvent>((byte[])value!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to decode mailbox.changed payload");
            return;
        }

        // only notify on new item adds
        if (evt is null || evt.Kind != MailboxChangeKind.Add)
            return;

        if (!_waiters.TryGetValue(evt.UserId, out var userWaiters))
            return;

        foreach (var w in userWaiters.Values)
            if (w.Collections.Contains(evt.CollectionId))
                w.Signal.TrySetResult();
    }

    public async Task WaitForChangeAsync(string userId, IReadOnlySet<string> collections, CancellationToken ct)
    {
        if (collections.Count == 0)
        {
            try { await Task.Delay(Timeout.Infinite, ct); } catch (OperationCanceledException) { }
            return;
        }

        var waiter = new Waiter(collections, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var id = Guid.NewGuid();
        var userWaiters = _waiters.GetOrAdd(userId, _ => new());
        userWaiters[id] = waiter;

        try
        {
            await using var reg = ct.Register(static s => ((TaskCompletionSource)s!).TrySetResult(), waiter.Signal);
            await waiter.Signal.Task;
        }
        finally
        {
            userWaiters.TryRemove(id, out _);
            if (userWaiters.IsEmpty)
                _waiters.TryRemove(userId, out _);
        }
    }
}
