using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Tests;

internal sealed class InMemorySessionStore : IMsnpGatewaySessionStore
{
    private readonly Dictionary<string, List<MsnpCommand>> outboxes = [];
    private readonly Dictionary<string, List<ChatDelivery>> chatQueues = [];

    public Dictionary<string, MsnpGatewaySession> Sessions { get; } = [];
    public HashSet<string> ParkedPolls { get; } = [];

    public void QueueChatDelivery(string sessionId, ChatDelivery delivery)
    {
        if (!chatQueues.TryGetValue(sessionId, out var queue))
            chatQueues[sessionId] = queue = [];

        queue.Add(delivery);
    }

    public Task<MsnpGatewaySession?> FindAsync(string sessionId, CancellationToken ct = default) =>
        Task.FromResult(Sessions.GetValueOrDefault(sessionId));

    public Task TouchAsync(MsnpGatewaySession session, CancellationToken ct = default) => Task.CompletedTask;

    public Task SaveAsync(MsnpGatewaySession session, CancellationToken ct = default)
    {
        Sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        Sessions.Remove(sessionId);
        outboxes.Remove(sessionId);
        return Task.CompletedTask;
    }

    public Task EnqueueAsync(MsnpGatewaySession session, IEnumerable<MsnpCommand> commands, CancellationToken ct = default)
    {
        if (!outboxes.TryGetValue(session.SessionId, out var outbox))
            outboxes[session.SessionId] = outbox = [];

        outbox.AddRange(commands);
        return Task.CompletedTask;
    }

    public Task<MsnpGatewayDrain> WaitAndDrainAsync(string sessionId, TimeSpan timeout, CancellationToken ct = default)
    {
        outboxes.Remove(sessionId, out var commands);
        chatQueues.Remove(sessionId, out var deliveries);

        return Task.FromResult(new MsnpGatewayDrain(commands ?? [], deliveries ?? []));
    }

    public Task RequeueAsync(MsnpGatewaySession session, MsnpGatewayDrain drain)
    {
        if (drain.Commands.Count > 0)
        {
            outboxes.TryGetValue(session.SessionId, out var outbox);
            outboxes[session.SessionId] = [.. drain.Commands, .. outbox ?? []];
        }

        if (drain.Deliveries.Count > 0)
        {
            chatQueues.TryGetValue(session.SessionId, out var queue);
            chatQueues[session.SessionId] = [.. drain.Deliveries, .. queue ?? []];
        }

        return Task.CompletedTask;
    }

    public Task<long> NotifyAsync(string sessionId) => Task.FromResult(ParkedPolls.Contains(sessionId) ? 1L : 0L);

    public Task<bool> HasPendingAsync(string sessionId) =>
        Task.FromResult(outboxes.ContainsKey(sessionId) || chatQueues.ContainsKey(sessionId));

    public Task<bool> TryClaimDoorbellAsync(string sessionId, TimeSpan window) => Task.FromResult(true);
}
