using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Nsp;

public class PresenceDirectory(IConnectionMultiplexer redis)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private static string Key(string deviceId) => $"push:presence:{deviceId}";

    public Task SetAsync(string deviceId, string instanceId) =>
        redis.GetDatabase().StringSetAsync(Key(deviceId), instanceId, Ttl);

    public async Task<string> GetOwnerAsync(string deviceId)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(deviceId));
        return value.IsNull ? null : value.ToString();
    }

    public Task RemoveAsync(string deviceId, string instanceId) =>
        redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, [(RedisKey)Key(deviceId)], [(RedisValue)instanceId]);
}
