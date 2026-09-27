using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Identity.Services;

public record SsoAuthorizationCodePayload(
    Guid UserId,
    Guid SessionId,
    string ClientId,
    string RedirectUri,
    string[] ServiceTargets,
    string? CodeChallenge,
    string? CodeChallengeMethod);

public interface ISsoAuthorizationCodeStore
{
    Task StoreAsync(string code, SsoAuthorizationCodePayload payload, TimeSpan ttl);

    Task<SsoAuthorizationCodePayload?> TakeAsync(string code);
}

public class RedisSsoAuthorizationCodeStore(IConnectionMultiplexer redis) : ISsoAuthorizationCodeStore
{
    private readonly IDatabase db = redis.GetDatabase();

    // keyed by the hash, so reading redis does not hand anyone a live code
    private static string Key(string code)
        => $"ssocode:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)))}";

    public Task StoreAsync(string code, SsoAuthorizationCodePayload payload, TimeSpan ttl)
        => db.StringSetAsync(Key(code), JsonSerializer.Serialize(payload), ttl);

    public async Task<SsoAuthorizationCodePayload?> TakeAsync(string code)
    {
        var raw = await db.StringGetDeleteAsync(Key(code));
        return raw.IsNull ? null : JsonSerializer.Deserialize<SsoAuthorizationCodePayload>((string)raw!);
    }
}
