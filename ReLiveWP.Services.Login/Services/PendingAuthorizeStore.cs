using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace ReLiveWP.Services.Login.Services;

public record PendingAuthorize(
    string ClientId,
    string RedirectUri,
    string State,
    string[] ServiceTargets,
    string? CodeChallenge,
    string? CodeChallengeMethod);

public class PendingAuthorizeStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly IDatabase db = redis.GetDatabase();

    private static string Key(string id) => $"ssopending:{id}";

    public async Task<string> CreateAsync(PendingAuthorize pending)
    {
        var id = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        await db.StringSetAsync(Key(id), JsonSerializer.Serialize(pending), Ttl);

        return id;
    }

    public async Task<PendingAuthorize?> PeekAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var value = await db.StringGetAsync(Key(id));
        return value.IsNull ? null : JsonSerializer.Deserialize<PendingAuthorize>((string)value!);
    }

    public async Task<PendingAuthorize?> TakeAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var value = await db.StringGetDeleteAsync(Key(id));
        return value.IsNull ? null : JsonSerializer.Deserialize<PendingAuthorize>((string)value!);
    }
}
