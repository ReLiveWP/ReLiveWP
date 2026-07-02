using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Data;

public class SessionStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    private static string SessionKey(string token) => $"push:session:{token}";
    private static string DeviceKey(string deviceId) => $"push:device-session:{deviceId}";

    private readonly IDatabase db = redis.GetDatabase();

    public async Task CreateAsync(string token, string deviceId, CancellationToken ct = default)
    {
        // one live session per device, evict whatever token we last issued to it
        var previous = await db.StringGetAsync(DeviceKey(deviceId));
        if (!previous.IsNull)
            await db.KeyDeleteAsync(SessionKey(previous));

        await db.StringSetAsync(SessionKey(token), deviceId, Ttl);
        await db.StringSetAsync(DeviceKey(deviceId), token, Ttl);
    }

    public async Task<DeviceSession> FindAsync(string token, CancellationToken ct = default)
    {
        var value = await db.StringGetAsync(SessionKey(token));
        return value.IsNull ? null : new DeviceSession { Token = token, DeviceId = value };
    }

    public Task TouchAsync(string token, CancellationToken ct = default) =>
        db.KeyExpireAsync(SessionKey(token), Ttl);
}
