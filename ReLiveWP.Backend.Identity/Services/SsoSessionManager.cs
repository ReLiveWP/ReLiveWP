using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public record SsoSessionCreation(string Handle, Guid SessionId, DateTimeOffset Expires);

public class SsoSessionManager(
    IConfiguration configuration,
    ILogger<SsoSessionManager> logger,
    LiveDbContext dbContext,
    TokenManager tokenManager,
    ISsoAuthorizationCodeStore codeStore)
{
    private static readonly TimeSpan CodeTtl = TimeSpan.FromSeconds(60);

    private TimeSpan PersistentSessionLifetime =>
        TimeSpan.FromDays(int.TryParse(configuration["Sso:PersistentSessionLifetimeDays"], out var days) ? days : 30);

    private TimeSpan TransientSessionLifetime =>
        TimeSpan.FromHours(int.TryParse(configuration["Sso:TransientSessionLifetimeHours"], out var hours) ? hours : 12);

    private TimeSpan SlidingWindow =>
        TimeSpan.FromDays(int.TryParse(configuration["Sso:SlidingWindowDays"], out var days) ? days : 7);

    public async Task<SsoSessionCreation> CreateSessionAsync(Guid userId, bool persistent, string? userAgent, string? createdIp)
    {
        var handle = "ss_" + Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        var absolute = now.Add(persistent ? PersistentSessionLifetime : TransientSessionLifetime);
        var expires = persistent ? Min(now.Add(SlidingWindow), absolute) : absolute;

        var session = new LiveSsoSession()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(handle),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = expires,
            AbsoluteExpiresAt = absolute,
            Persistent = persistent,
            UserAgent = Truncate(userAgent, 512),
            CreatedIp = Truncate(createdIp, 64),
        };

        dbContext.LiveSsoSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return new SsoSessionCreation(handle, session.Id, absolute);
    }

    public async Task<LiveSsoSession?> ValidateSessionAsync(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return null;

        var hash = Hash(handle);
        var now = DateTimeOffset.UtcNow;

        var session = await dbContext.LiveSsoSessions.FirstOrDefaultAsync(s => s.TokenHash == hash);
        if (session == null || session.RevokedAt != null)
            return null;

        if (session.ExpiresAt <= now || session.AbsoluteExpiresAt <= now)
            return null;

        session.LastSeenAt = now;
        var slid = Min(now.Add(SlidingWindow), session.AbsoluteExpiresAt);
        if (slid > session.ExpiresAt)
            session.ExpiresAt = slid;

        await dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<bool> RevokeSessionAsync(string handle)
    {
        var hash = Hash(handle);
        var session = await dbContext.LiveSsoSessions.FirstOrDefaultAsync(s => s.TokenHash == hash);
        if (session == null || session.RevokedAt != null)
            return false;

        session.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        await tokenManager.RevokeTokensForSessionAsync(session.Id);

        return true;
    }

    public Task<List<LiveSsoSession>> ListSessionsAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.LiveSsoSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.AbsoluteExpiresAt > now)
            .OrderByDescending(s => s.LastSeenAt)
            .ToListAsync();
    }

    public async Task<string> IssueAuthorizationCodeAsync(SsoAuthorizationCodePayload payload)
    {
        var code = "ac_" + Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        await codeStore.StoreAsync(code, payload, CodeTtl);

        return code;
    }

    public async Task<SsoAuthorizationCodePayload?> RedeemAuthorizationCodeAsync(
        string code, string clientId, string redirectUri, string? codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var payload = await codeStore.TakeAsync(code);
        if (payload == null)
            return null;

        if (!FixedTimeEquals(payload.ClientId, clientId) || !FixedTimeEquals(payload.RedirectUri, redirectUri))
        {
            logger.LogWarning("Authorization code redeemed with mismatched client or redirect uri");
            return null;
        }

        if (!VerifyPkce(payload, codeVerifier))
        {
            logger.LogWarning("Authorization code redeemed with a bad PKCE verifier");
            return null;
        }

        var session = await dbContext.LiveSsoSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == payload.SessionId);
        if (session == null || session.RevokedAt != null || session.AbsoluteExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        return payload;
    }

    internal static bool VerifyPkce(SsoAuthorizationCodePayload payload, string? codeVerifier)
    {
        if (payload.CodeChallenge is not { Length: > 0 } challenge)
            return true;

        if (string.IsNullOrEmpty(codeVerifier))
            return false;

        var presented = payload.CodeChallengeMethod == "plain"
            ? codeVerifier
            : Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        return FixedTimeEquals(challenge, presented);
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    private static string? Truncate(string? value, int length)
        => value is { Length: > 0 } ? value[..Math.Min(value.Length, length)] : null;
}
