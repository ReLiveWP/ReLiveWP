using StackExchange.Redis;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class SyncCursorStore(IConnectionMultiplexer redis, ILogger<SyncCursorStore> logger)
{
    // the client truncates SyncToken at 256 characters, so it only ever carries a handle
    public const int MaxTokenLength = 256;

    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    private static string Key(string token) => $"skydocs:cursor:{token}";

    private readonly IDatabase db = redis.GetDatabase();

    public async Task<string> IssueAsync(string serviceId, string cursor)
    {
        var token = Guid.NewGuid().ToString("N");
        try
        {
            await db.StringSetAsync(Key(token), $"{serviceId}\n{cursor}", Ttl);
        }
        catch (RedisException ex)
        {
            throw RedisFaults.Unavailable(ex);
        }

        return token;
    }

    public async Task<string?> ResolveAsync(string? token, string serviceId)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        RedisValue value;
        try
        {
            value = await db.StringGetAsync(Key(token));
        }
        catch (RedisException ex)
        {
            throw RedisFaults.Unavailable(ex);
        }

        if (value.IsNull)
            return null;

        var stored = (string)value!;
        var separator = stored.IndexOf('\n');
        if (separator < 0)
        {
            logger.LogWarning("Sync cursor {Token} is malformed, forcing a full resync", token);
            return null;
        }

        var issuedFor = stored[..separator];
        if (issuedFor != serviceId)
        {
            logger.LogInformation("Sync cursor {Token} was issued for {IssuedFor}, not {ServiceId}, forcing a full resync",
                token, issuedFor, serviceId);
            return null;
        }

        return stored[(separator + 1)..];
    }
}
