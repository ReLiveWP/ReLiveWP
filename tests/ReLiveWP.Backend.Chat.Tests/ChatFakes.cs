using Grpc.Core;
using RedLockNet;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Tests;

internal sealed class FakeAudienceSource : IAudienceSource
{
    private readonly HashSet<(string Viewer, string Target)> sightlines = [];

    public bool Unavailable { get; set; }

    public void LetSee(string viewer, string target) => sightlines.Add((viewer, target));

    public void StopSeeing(string viewer, string target) => sightlines.Remove((viewer, target));

    public void MakeMutual(string first, string second)
    {
        LetSee(first, second);
        LetSee(second, first);
    }

    public void BreakMutual(string first, string second)
    {
        StopSeeing(first, second);
        StopSeeing(second, first);
    }

    public Task<PresenceAudience> GetAudienceAsync(string userId, CancellationToken ct)
    {
        if (Unavailable)
            throw new RpcException(new Status(StatusCode.Unavailable, "mailbox is down"));

        var audience = new PresenceAudience(
            CanSee: sightlines.Where(s => s.Viewer == userId).Select(s => s.Target).ToHashSet(),
            SeenBy: sightlines.Where(s => s.Target == userId).Select(s => s.Viewer).ToHashSet());

        return Task.FromResult(audience);
    }
}

internal sealed class RecordingDeliveryQueue : IDeliveryQueue
{
    private readonly List<(string EndpointId, ChatDelivery Delivery)> queued = [];
    private readonly List<string> overflowing = [];

    public List<string> Cleared { get; } = [];

    public int MaxQueuedPerEndpoint { get; set; } = int.MaxValue;

    public int CountFor(string endpointId) => queued.Count(q => q.EndpointId == endpointId);

    public bool HasRoomFor(string endpointId, int count) => CountFor(endpointId) + count <= MaxQueuedPerEndpoint;

    public void MarkOverflowing(string endpointId)
    {
        if (!overflowing.Contains(endpointId))
            overflowing.Add(endpointId);
    }

    public IReadOnlyList<(string Address, PresenceStatus Status)> PresenceFor(string endpointId) =>
        queued.Where(q => q.EndpointId == endpointId && q.Delivery.KindCase == ChatDelivery.KindOneofCase.PresenceChanged)
            .Select(q => (q.Delivery.PresenceChanged.Address, q.Delivery.PresenceChanged.Status))
            .ToList();

    public IReadOnlyList<PresenceChanged> PresenceChangesFor(string endpointId) =>
        queued.Where(q => q.EndpointId == endpointId && q.Delivery.KindCase == ChatDelivery.KindOneofCase.PresenceChanged)
            .Select(q => q.Delivery.PresenceChanged)
            .ToList();

    public IReadOnlyList<string> MessagesFor(string endpointId) =>
        queued.Where(q => q.EndpointId == endpointId && q.Delivery.KindCase == ChatDelivery.KindOneofCase.MessageReceived)
            .Select(q => q.Delivery.MessageReceived.Payload.ToStringUtf8())
            .ToList();

    public IReadOnlyList<MessageReceived> ReceivedFor(string endpointId) =>
        queued.Where(q => q.EndpointId == endpointId && q.Delivery.KindCase == ChatDelivery.KindOneofCase.MessageReceived)
            .Select(q => q.Delivery.MessageReceived)
            .ToList();

    public bool IsEmpty => queued.Count == 0;

    public void Reset() => queued.Clear();

    public Task<bool> EnqueueAsync(ChatEndpoint target, ChatDelivery delivery)
    {
        if (!HasRoomFor(target.EndpointId, 1))
        {
            MarkOverflowing(target.EndpointId);
            return Task.FromResult(false);
        }

        queued.Add((target.EndpointId, delivery));
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> ClaimOverflowingAsync(DateTimeOffset now)
    {
        IReadOnlyList<string> claimed = [.. overflowing];
        overflowing.Clear();
        return Task.FromResult(claimed);
    }

    public Task ScheduleOverflowEvictionAsync(string endpointId, DateTimeOffset dueAt)
    {
        MarkOverflowing(endpointId);
        return Task.CompletedTask;
    }

    public Task ClearAsync(string endpointId)
    {
        Cleared.Add(endpointId);
        queued.RemoveAll(q => q.EndpointId == endpointId);
        return Task.CompletedTask;
    }

    public Task<int> MoveMessagesAsync(string fromEndpointId, ChatEndpoint target)
    {
        var messages = queued
            .Where(q => q.EndpointId == fromEndpointId && q.Delivery.KindCase == ChatDelivery.KindOneofCase.MessageReceived)
            .Select(q => q.Delivery)
            .ToList();

        queued.RemoveAll(q => q.EndpointId == fromEndpointId);
        queued.AddRange(messages.Select(m => (target.EndpointId, m)));
        return Task.FromResult(messages.Count);
    }
}

internal sealed class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<string, string> users = new(StringComparer.OrdinalIgnoreCase);

    public bool Unavailable { get; set; }

    public void Add(string address, string userId) => users[address] = userId;

    public Task<string?> FindUserIdByAddressAsync(string address, CancellationToken ct)
    {
        if (Unavailable)
            throw new RpcException(new Status(StatusCode.Unavailable, "identity is down"));

        return Task.FromResult(users.GetValueOrDefault(address));
    }
}

internal sealed class InMemoryStoredMessageStore(RecordingDeliveryQueue deliveries, ChatOptions options) : IStoredMessageStore
{
    private readonly Dictionary<string, List<(string SenderId, ChatDelivery Delivery)>> stored = [];

    public IReadOnlyList<string> WaitingFor(string recipientId) =>
        stored.GetValueOrDefault(recipientId)?.Select(s => s.Delivery.MessageReceived.Payload.ToStringUtf8()).ToList() ?? [];

    public Task<bool> TryStoreAsync(string recipientId, string senderId, ChatDelivery delivery)
    {
        if (!stored.TryGetValue(recipientId, out var waiting))
            stored[recipientId] = waiting = [];

        if (waiting.Count >= options.MaxStoredPerRecipient || waiting.Count(w => w.SenderId == senderId) >= options.MaxStoredPerPair)
            return Task.FromResult(false);

        waiting.Add((senderId, delivery));
        return Task.FromResult(true);
    }

    public async Task<int> DeliverStoredAsync(string recipientId, IReadOnlyList<ChatEndpoint> targets)
    {
        if (targets.Count == 0 || !stored.TryGetValue(recipientId, out var waiting))
            return 0;

        var delivered = 0;
        foreach (var target in targets)
        {
            if (!deliveries.HasRoomFor(target.EndpointId, waiting.Count))
            {
                deliveries.MarkOverflowing(target.EndpointId);
                continue;
            }

            foreach (var (_, delivery) in waiting)
                await deliveries.EnqueueAsync(target, delivery);

            delivered++;
        }

        if (delivered == 0)
            return 0;

        stored.Remove(recipientId);
        return waiting.Count;
    }

    public Task<int> DropExpiredAsync(DateTimeOffset now)
    {
        var cutoff = (now - options.StoredMessageRetention).ToUnixTimeMilliseconds();
        var dropped = 0;
        foreach (var waiting in stored.Values)
            dropped += waiting.RemoveAll(w => w.Delivery.MessageReceived.OriginalArrivalUnixMs <= cutoff);

        return Task.FromResult(dropped);
    }
}

internal sealed class AlwaysAcquiredLockFactory : IDistributedLockFactory
{
    public IRedLock CreateLock(string resource, TimeSpan expiryTime) => new HeldLock(resource);

    public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime) =>
        Task.FromResult<IRedLock>(new HeldLock(resource));

    public IRedLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null) =>
        new HeldLock(resource);

    public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null) =>
        Task.FromResult<IRedLock>(new HeldLock(resource));

    private sealed class HeldLock(string resource) : IRedLock
    {
        public string Resource { get; } = resource;
        public string LockId { get; } = Guid.NewGuid().ToString("N");
        public bool IsAcquired => true;
        public RedLockStatus Status => RedLockStatus.Acquired;
        public RedLockInstanceSummary InstanceSummary => default!;
        public int ExtendCount => 0;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = start;

    public void Advance(TimeSpan by) => Now += by;

    public override DateTimeOffset GetUtcNow() => Now;
}
