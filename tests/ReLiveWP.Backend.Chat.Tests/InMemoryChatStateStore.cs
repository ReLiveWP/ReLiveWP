using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Tests;

internal sealed class InMemoryChatStateStore : IChatStateStore
{
    private readonly Dictionary<string, ChatEndpoint> endpoints = [];
    private readonly Dictionary<string, HashSet<string>> userEndpoints = [];
    private readonly Dictionary<string, DateTimeOffset> quietAt = [];
    private readonly Dictionary<string, DateTimeOffset> expiresAt = [];
    private readonly Dictionary<string, ChatUser> users = [];
    private readonly Dictionary<string, (HashSet<string> CanSee, HashSet<string> SeenBy)> audiences = [];
    private readonly Dictionary<string, string> addresses = [];
    private readonly Dictionary<(string, string), SendOutcome> sendOutcomes = [];
    private readonly Dictionary<string, (double Tokens, DateTimeOffset Stamp)> buckets = [];

    public bool HasEndpoint(string endpointId) => endpoints.ContainsKey(endpointId);

    public void ForgetAudience(string userId) => audiences.Remove(userId);

    public Task SaveEndpointAsync(ChatEndpoint endpoint)
    {
        endpoints[endpoint.EndpointId] = endpoint;
        addresses[endpoint.Address.Trim().ToLowerInvariant()] = endpoint.UserId;
        if (!userEndpoints.TryGetValue(endpoint.UserId, out var set))
            userEndpoints[endpoint.UserId] = set = [];

        set.Add(endpoint.EndpointId);
        return Task.CompletedTask;
    }

    private readonly Dictionary<string, int> failingFinds = [];

    public void FailFinds(string endpointId, int times) => failingFinds[endpointId] = times;

    public Task<ChatEndpoint?> FindEndpointAsync(string endpointId)
    {
        if (failingFinds.TryGetValue(endpointId, out var failuresLeft) && failuresLeft > 0)
        {
            failingFinds[endpointId] = failuresLeft - 1;
            throw new InvalidOperationException($"reading {endpointId} failed");
        }

        return Task.FromResult(endpoints.GetValueOrDefault(endpointId));
    }

    public Task<IReadOnlyList<ChatEndpoint>> ListEndpointsAsync(string userId)
    {
        IReadOnlyList<ChatEndpoint> list = userEndpoints.TryGetValue(userId, out var set)
            ? set.Order().Select(id => endpoints[id]).ToList()
            : [];

        return Task.FromResult(list);
    }

    public Task RemoveEndpointAsync(ChatEndpoint endpoint)
    {
        endpoints.Remove(endpoint.EndpointId);
        quietAt.Remove(endpoint.EndpointId);
        expiresAt.Remove(endpoint.EndpointId);
        if (userEndpoints.TryGetValue(endpoint.UserId, out var set))
            set.Remove(endpoint.EndpointId);

        return Task.CompletedTask;
    }

    private readonly Dictionary<string, EndpointState> gone = [];

    public Task MarkGoneAsync(ChatEndpoint endpoint, EndpointState reason)
    {
        gone[endpoint.EndpointId] = reason;
        return Task.CompletedTask;
    }

    public Task<EndpointState> FindGoneReasonAsync(string endpointId) =>
        Task.FromResult(gone.GetValueOrDefault(endpointId, EndpointState.Unknown));

    private readonly Dictionary<string, DateTimeOffset> presenceAnnouncements = [];

    public Task SchedulePresenceAnnouncementAsync(string userId, DateTimeOffset dueAt)
    {
        presenceAnnouncements.TryAdd(userId, dueAt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ClaimPresenceAnnouncementsAsync(DateTimeOffset now) =>
        Task.FromResult(ClaimDue(presenceAnnouncements, now));

    public Task TouchEndpointAsync(ChatEndpoint endpoint, DateTimeOffset now, TimeSpan activeWindow)
    {
        if (!endpoints.TryGetValue(endpoint.EndpointId, out var stored))
            return Task.CompletedTask;

        endpoints[endpoint.EndpointId] = stored with { LastActivity = now };
        quietAt[endpoint.EndpointId] = now + activeWindow;
        expiresAt[endpoint.EndpointId] = now + endpoint.SessionTimeout;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ClaimQuietDueAsync(DateTimeOffset now) => Task.FromResult(ClaimDue(quietAt, now));

    public Task<IReadOnlyList<string>> ClaimExpiredAsync(DateTimeOffset now) => Task.FromResult(ClaimDue(expiresAt, now));

    public Task ScheduleQuietCheckAsync(string endpointId, DateTimeOffset dueAt)
    {
        quietAt.TryAdd(endpointId, dueAt);
        return Task.CompletedTask;
    }

    public Task ScheduleExpiryCheckAsync(string endpointId, DateTimeOffset dueAt)
    {
        expiresAt.TryAdd(endpointId, dueAt);
        return Task.CompletedTask;
    }

    public Task<long> CountEndpointsAsync() => Task.FromResult((long)expiresAt.Count);

    private static IReadOnlyList<string> ClaimDue(Dictionary<string, DateTimeOffset> schedule, DateTimeOffset now)
    {
        var due = schedule.Where(pair => pair.Value <= now).Select(pair => pair.Key).Order().ToList();
        foreach (var id in due)
            schedule.Remove(id);

        return due;
    }

    public Task<ChatUser?> FindUserAsync(string userId) => Task.FromResult(users.GetValueOrDefault(userId));

    public Task SaveUserAsync(ChatUser user, TimeSpan lifetime)
    {
        users[user.UserId] = user;
        return Task.CompletedTask;
    }

    public Task<string?> FindUserIdByAddressAsync(string address) =>
        Task.FromResult(addresses.GetValueOrDefault(address.Trim().ToLowerInvariant()));

    public Task ForgetUserAsync(ChatUser user)
    {
        users.Remove(user.UserId);
        var addressKey = user.Address.Trim().ToLowerInvariant();
        if (addresses.GetValueOrDefault(addressKey) == user.UserId)
            addresses.Remove(addressKey);

        return Task.CompletedTask;
    }

    public Task<SendOutcome?> FindSendOutcomeAsync(string endpointId, string transactionId) =>
        Task.FromResult(sendOutcomes.TryGetValue((endpointId, transactionId), out var outcome) ? outcome : (SendOutcome?)null);

    public Task RecordSendOutcomeAsync(string endpointId, string transactionId, SendOutcome outcome, TimeSpan ttl)
    {
        sendOutcomes[(endpointId, transactionId)] = outcome;
        return Task.CompletedTask;
    }

    public Task<bool> TryTakeTokensAsync(string bucket, double cost, double burst, double refillPerSecond, DateTimeOffset now)
    {
        var (tokens, stamp) = buckets.GetValueOrDefault(bucket, (burst, now));
        tokens = Math.Min(burst, tokens + Math.Max(0, (now - stamp).TotalSeconds) * refillPerSecond);

        var taken = tokens >= cost;
        if (taken)
            tokens -= cost;

        buckets[bucket] = (tokens, now);
        return Task.FromResult(taken);
    }

    public Task<PresenceAudience?> GetAudienceAsync(string userId) =>
        Task.FromResult(audiences.TryGetValue(userId, out var cached)
            ? new PresenceAudience(cached.CanSee.ToHashSet(), cached.SeenBy.ToHashSet())
            : null);

    public Task SetAudienceAsync(string userId, PresenceAudience audience, TimeSpan ttl)
    {
        audiences[userId] = (audience.CanSee.ToHashSet(), audience.SeenBy.ToHashSet());
        return Task.CompletedTask;
    }

    public Task AddAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member)
    {
        if (audiences.TryGetValue(userId, out var cached))
            Side(cached, side).Add(member);

        return Task.CompletedTask;
    }

    public Task RemoveAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member)
    {
        if (audiences.TryGetValue(userId, out var cached))
            Side(cached, side).Remove(member);

        return Task.CompletedTask;
    }

    private static HashSet<string> Side((HashSet<string> CanSee, HashSet<string> SeenBy) cached, AudienceSide side) =>
        side == AudienceSide.CanSee ? cached.CanSee : cached.SeenBy;
}
