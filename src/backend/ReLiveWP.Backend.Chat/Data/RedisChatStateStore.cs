using ReLiveWP.Services.Grpc.Chat;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Chat.Data;

public class RedisChatStateStore(IConnectionMultiplexer redis) : IChatStateStore
{
    private const string QuietKey = "chat:quiet";
    private const string ExpiryKey = "chat:expiry";
    private const string PresenceAnnounceKey = "chat:presence-due";

    private static readonly TimeSpan ExpiryGrace = TimeSpan.FromHours(1);

    // redis has no empty set, so a cached audience always holds this marker next to its members
    private const string AudienceMarker = "";

    private const string ClaimDueScript = """
        local due = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, 100)
        if #due > 0 then redis.call('ZREM', KEYS[1], unpack(due)) end
        return due
        """;

    private const string AddIfExistsScript = """
        if redis.call('EXISTS', KEYS[1]) == 1 then return redis.call('SADD', KEYS[1], ARGV[1]) end
        return 0
        """;

    private const string TakeTokensScript = """
        local cost = tonumber(ARGV[1])
        local burst = tonumber(ARGV[2])
        local refill = tonumber(ARGV[3])
        local now = tonumber(ARGV[4])
        local tokens = tonumber(redis.call('HGET', KEYS[1], 'tokens'))
        local stamp = tonumber(redis.call('HGET', KEYS[1], 'stamp'))
        if tokens == nil or stamp == nil then
            tokens = burst
            stamp = now
        end
        tokens = math.min(burst, tokens + math.max(0, now - stamp) / 1000 * refill)
        local taken = 0
        if tokens >= cost then
            tokens = tokens - cost
            taken = 1
        end
        redis.call('HSET', KEYS[1], 'tokens', tostring(tokens), 'stamp', tostring(now))
        redis.call('PEXPIRE', KEYS[1], math.ceil(burst / refill * 1000) + 1000)
        return taken
        """;

    private readonly IDatabase db = redis.GetDatabase();

    private static RedisKey EndpointKey(string endpointId) => $"chat:endpoint:{endpointId}";
    private static RedisKey AddressKey(string address) => $"chat:address:{address.Trim().ToLowerInvariant()}";
    private static RedisKey SendKey(string endpointId, string transactionId) => $"chat:sent:{endpointId}:{transactionId}";
    private static RedisKey TokenBucketKey(string bucket) => $"chat:rate:{bucket}";
    private static RedisKey GoneKey(string endpointId) => $"chat:gone:{endpointId}";
    private static RedisKey UserEndpointsKey(string userId) => $"chat:user:{userId}:endpoints";
    private static RedisKey UserKey(string userId) => $"chat:user:{userId}";
    private static RedisKey AudienceKey(string userId, AudienceSide side) => side switch
    {
        AudienceSide.CanSee => $"chat:can-see:{userId}",
        _ => $"chat:seen-by:{userId}",
    };

    public async Task SaveEndpointAsync(ChatEndpoint endpoint)
    {
        HashEntry[] fields =
        [
            new("user", endpoint.UserId),
            new("address", endpoint.Address),
            new("status", (int)endpoint.Status),
            new("active", endpoint.Active ? 1 : 0),
            new("timeout", (long)endpoint.SessionTimeout.TotalSeconds),
            new("lastActivity", endpoint.LastActivity.ToUnixTimeMilliseconds()),
            new("instance", endpoint.InstanceId),
        ];

        var transaction = db.CreateTransaction();
        _ = transaction.HashSetAsync(EndpointKey(endpoint.EndpointId), fields);
        _ = transaction.SetAddAsync(UserEndpointsKey(endpoint.UserId), endpoint.EndpointId);
        _ = transaction.StringSetAsync(AddressKey(endpoint.Address), endpoint.UserId, keepTtl: true);
        ExtendLifetime(transaction, endpoint);
        await transaction.ExecuteAsync();
    }

    public Task MarkGoneAsync(ChatEndpoint endpoint, EndpointState reason) =>
        db.StringSetAsync(GoneKey(endpoint.EndpointId), (int)reason, endpoint.SessionTimeout + ExpiryGrace);

    public async Task<EndpointState> FindGoneReasonAsync(string endpointId)
    {
        var reason = await db.StringGetAsync(GoneKey(endpointId));
        return reason.IsNullOrEmpty ? EndpointState.Unknown : (EndpointState)(int)reason;
    }

    public async Task TouchEndpointAsync(ChatEndpoint endpoint, DateTimeOffset now, TimeSpan activeWindow)
    {
        var quietAt = now + activeWindow;
        var expiresAt = now + endpoint.SessionTimeout;

        var transaction = db.CreateTransaction();
        transaction.AddCondition(Condition.KeyExists(EndpointKey(endpoint.EndpointId)));
        _ = transaction.HashSetAsync(EndpointKey(endpoint.EndpointId), "lastActivity", now.ToUnixTimeMilliseconds());
        _ = transaction.SortedSetAddAsync(QuietKey, endpoint.EndpointId, quietAt.ToUnixTimeMilliseconds());
        _ = transaction.SortedSetAddAsync(ExpiryKey, endpoint.EndpointId, expiresAt.ToUnixTimeMilliseconds());
        ExtendLifetime(transaction, endpoint);
        await transaction.ExecuteAsync();
    }

    // keys outlive the expiry deadline by the grace, so the sweeper can still read what it's ending.
    // user keys follow their longest endpoint, NX covers the fresh key GT can't see
    private static void ExtendLifetime(ITransaction transaction, ChatEndpoint endpoint)
    {
        var lifetime = endpoint.SessionTimeout + ExpiryGrace;
        _ = transaction.KeyExpireAsync(EndpointKey(endpoint.EndpointId), lifetime);

        foreach (var userKey in new[] { UserEndpointsKey(endpoint.UserId), UserKey(endpoint.UserId), AddressKey(endpoint.Address) })
            ExtendExpiry(transaction, userKey, lifetime);
    }

    private static void ExtendExpiry(ITransaction transaction, RedisKey key, TimeSpan lifetime)
    {
        _ = transaction.KeyExpireAsync(key, lifetime, ExpireWhen.HasNoExpiry);
        _ = transaction.KeyExpireAsync(key, lifetime, ExpireWhen.GreaterThanCurrentExpiry);
    }

    public async Task<ChatEndpoint?> FindEndpointAsync(string endpointId)
    {
        var fields = await db.HashGetAllAsync(EndpointKey(endpointId));
        return ReadEndpoint(endpointId, fields);
    }

    public async Task<IReadOnlyList<ChatEndpoint>> ListEndpointsAsync(string userId)
    {
        var endpointIds = await db.SetMembersAsync(UserEndpointsKey(userId));
        var lookups = endpointIds.Select(id => FindEndpointAsync(id!)).ToArray();
        var found = await Task.WhenAll(lookups);

        var endpoints = new List<ChatEndpoint>(endpointIds.Length);
        for (var i = 0; i < endpointIds.Length; i++)
        {
            if (found[i] is not { } endpoint)
            {
                await db.SetRemoveAsync(UserEndpointsKey(userId), endpointIds[i]);
                continue;
            }

            endpoints.Add(endpoint);
        }

        return endpoints;
    }

    public async Task RemoveEndpointAsync(ChatEndpoint endpoint)
    {
        var transaction = db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(EndpointKey(endpoint.EndpointId));
        _ = transaction.SetRemoveAsync(UserEndpointsKey(endpoint.UserId), endpoint.EndpointId);
        _ = transaction.SortedSetRemoveAsync(QuietKey, endpoint.EndpointId);
        _ = transaction.SortedSetRemoveAsync(ExpiryKey, endpoint.EndpointId);
        await transaction.ExecuteAsync();
    }

    public Task<IReadOnlyList<string>> ClaimQuietDueAsync(DateTimeOffset now) => ClaimDueAsync(QuietKey, now);

    public Task<IReadOnlyList<string>> ClaimExpiredAsync(DateTimeOffset now) => ClaimDueAsync(ExpiryKey, now);

    public Task ScheduleQuietCheckAsync(string endpointId, DateTimeOffset dueAt) =>
        db.SortedSetAddAsync(QuietKey, endpointId, dueAt.ToUnixTimeMilliseconds(), SortedSetWhen.NotExists);

    public Task ScheduleExpiryCheckAsync(string endpointId, DateTimeOffset dueAt) =>
        db.SortedSetAddAsync(ExpiryKey, endpointId, dueAt.ToUnixTimeMilliseconds(), SortedSetWhen.NotExists);

    public Task SchedulePresenceAnnouncementAsync(string userId, DateTimeOffset dueAt) =>
        db.SortedSetAddAsync(PresenceAnnounceKey, userId, dueAt.ToUnixTimeMilliseconds(), SortedSetWhen.NotExists);

    public Task<IReadOnlyList<string>> ClaimPresenceAnnouncementsAsync(DateTimeOffset now) => ClaimDueAsync(PresenceAnnounceKey, now);

    public Task<long> CountEndpointsAsync() => db.SortedSetLengthAsync(ExpiryKey);

    private Task<IReadOnlyList<string>> ClaimDueAsync(RedisKey key, DateTimeOffset now) => ClaimDueAsync(db, key, now);

    internal static async Task<IReadOnlyList<string>> ClaimDueAsync(IDatabase db, RedisKey key, DateTimeOffset now)
    {
        var result = await db.ScriptEvaluateAsync(ClaimDueScript, [key], [now.ToUnixTimeMilliseconds()]);
        var members = (RedisValue[]?)result ?? [];
        return members.Select(m => (string)m!).ToList();
    }

    public async Task<ChatUser?> FindUserAsync(string userId)
    {
        var fields = await db.HashGetAllAsync(UserKey(userId));
        var values = fields.ToDictionary(f => (string)f.Name!, f => f.Value);

        if (!values.TryGetValue("address", out var address) || !values.TryGetValue("shown", out var shown))
            return null;

        var chosen = values.TryGetValue("chosen", out var chosenValue) ? (PresenceStatus)(int)chosenValue : PresenceStatus.Online;
        return new ChatUser(userId, address!, (PresenceStatus)(int)shown, chosen);
    }

    public async Task SaveUserAsync(ChatUser user, TimeSpan lifetime)
    {
        var userKey = UserKey(user.UserId);
        var transaction = db.CreateTransaction();
        _ = transaction.HashSetAsync(userKey,
            [new("address", user.Address), new("shown", (int)user.Shown), new("chosen", (int)user.Chosen)]);
        ExtendExpiry(transaction, userKey, lifetime + ExpiryGrace);
        await transaction.ExecuteAsync();
    }

    public async Task<string?> FindUserIdByAddressAsync(string address)
    {
        var userId = await db.StringGetAsync(AddressKey(address));
        return userId.IsNullOrEmpty ? null : (string?)userId;
    }

    public async Task ForgetUserAsync(ChatUser user)
    {
        await db.KeyDeleteAsync(UserKey(user.UserId));

        var transaction = db.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(AddressKey(user.Address), user.UserId));
        _ = transaction.KeyDeleteAsync(AddressKey(user.Address));
        await transaction.ExecuteAsync();
    }

    public async Task<SendOutcome?> FindSendOutcomeAsync(string endpointId, string transactionId)
    {
        var outcome = await db.StringGetAsync(SendKey(endpointId, transactionId));
        return outcome.IsNullOrEmpty ? null : (SendOutcome)(int)outcome;
    }

    public Task RecordSendOutcomeAsync(string endpointId, string transactionId, SendOutcome outcome, TimeSpan ttl) =>
        db.StringSetAsync(SendKey(endpointId, transactionId), (int)outcome, ttl);

    public async Task<bool> TryTakeTokensAsync(string bucket, double cost, double burst, double refillPerSecond, DateTimeOffset now)
    {
        var result = await db.ScriptEvaluateAsync(
            TakeTokensScript, [TokenBucketKey(bucket)], [cost, burst, refillPerSecond, now.ToUnixTimeMilliseconds()]);

        return (int)result == 1;
    }

    public async Task<PresenceAudience?> GetAudienceAsync(string userId)
    {
        var canSee = await ReadAudienceSideAsync(AudienceKey(userId, AudienceSide.CanSee));
        var seenBy = await ReadAudienceSideAsync(AudienceKey(userId, AudienceSide.SeenBy));

        return canSee is null || seenBy is null ? null : new PresenceAudience(canSee, seenBy);
    }

    private async Task<IReadOnlySet<string>?> ReadAudienceSideAsync(RedisKey key)
    {
        var members = await db.SetMembersAsync(key);
        if (members.Length == 0)
            return null;

        return members
            .Select(m => (string)m!)
            .Where(m => m != AudienceMarker)
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task SetAudienceAsync(string userId, PresenceAudience audience, TimeSpan ttl)
    {
        var transaction = db.CreateTransaction();
        WriteAudienceSide(transaction, AudienceKey(userId, AudienceSide.CanSee), audience.CanSee, ttl);
        WriteAudienceSide(transaction, AudienceKey(userId, AudienceSide.SeenBy), audience.SeenBy, ttl);
        await transaction.ExecuteAsync();
    }

    private static void WriteAudienceSide(ITransaction transaction, RedisKey key, IReadOnlySet<string> userIds, TimeSpan ttl)
    {
        RedisValue[] members = [AudienceMarker, .. userIds.Select(m => (RedisValue)m)];
        _ = transaction.KeyDeleteAsync(key);
        _ = transaction.SetAddAsync(key, members);
        _ = transaction.KeyExpireAsync(key, ttl);
    }

    public Task AddAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member) =>
        db.ScriptEvaluateAsync(AddIfExistsScript, [AudienceKey(userId, side)], [member]);

    public Task RemoveAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member) =>
        db.SetRemoveAsync(AudienceKey(userId, side), member);

    private static ChatEndpoint? ReadEndpoint(string endpointId, HashEntry[] fields)
    {
        if (fields.Length == 0)
            return null;

        var values = fields.ToDictionary(f => (string)f.Name!, f => f.Value);
        if (!values.TryGetValue("user", out var userId) || !values.TryGetValue("address", out var address))
            return null;

        return new ChatEndpoint(
            endpointId,
            userId!,
            address!,
            (PresenceStatus)(int)values.GetValueOrDefault("status", 0),
            (int)values.GetValueOrDefault("active", 0) == 1,
            TimeSpan.FromSeconds((long)values.GetValueOrDefault("timeout", 0)),
            DateTimeOffset.FromUnixTimeMilliseconds((long)values.GetValueOrDefault("lastActivity", 0)),
            (string?)values.GetValueOrDefault("instance", RedisValue.EmptyString) ?? "");
    }
}
