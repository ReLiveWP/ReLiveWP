using System.Text.Json;
using ReLiveWP.Backend.ConnectedServices.Data;
using StackExchange.Redis;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class PendingOAuthStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan MaxTtl = TimeSpan.FromMinutes(5);

    private static string Key(string state) => $"pendingoauth:{state}";

    private readonly IDatabase db = redis.GetDatabase();

    public Task SetAsync(LivePendingOAuth pending)
    {
        // honour the ticket's own expiry, capped so a bad ExpiresAt can't pin a key indefinitely
        var ttl = pending.ExpiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero || ttl > MaxTtl)
            ttl = MaxTtl;

        return db.StringSetAsync(Key(pending.State), JsonSerializer.Serialize(pending), ttl);
    }

    public async Task<LivePendingOAuth?> GetAsync(string state)
    {
        var value = await db.StringGetAsync(Key(state));
        return value.IsNull ? null : JsonSerializer.Deserialize<LivePendingOAuth>((string)value!);
    }

    public Task RemoveAsync(string state) => db.KeyDeleteAsync(Key(state));
}
