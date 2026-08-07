using StackExchange.Redis;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class SyncCursorStore(IConnectionMultiplexer redis)
{
    // the client truncates SyncToken at 256 characters, so it only ever carries a handle
    public const int MaxTokenLength = 256;

    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    private static string Key(string token) => $"skydocs:cursor:{token}";

    private readonly IDatabase db = redis.GetDatabase();

    public async Task<string> IssueAsync(string serviceId, string cursor)
    {
        var token = Guid.NewGuid().ToString("N");
        await db.StringSetAsync(Key(token), $"{serviceId}\n{cursor}", Ttl);
        return token;
    }

    public async Task<string?> ResolveAsync(string? token, string serviceId)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var value = await db.StringGetAsync(Key(token));
        if (value.IsNull)
            return null;

        var stored = (string)value!;
        var separator = stored.IndexOf('\n');
        if (separator < 0)
            return null;

        return stored[..separator] == serviceId ? stored[(separator + 1)..] : null;
    }
}
