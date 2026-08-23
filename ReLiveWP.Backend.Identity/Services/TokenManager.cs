using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Identity.LiveID;
using ReLiveWP.Passport;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Services;

public record struct SecurityToken(string Token, DateTimeOffset Created, DateTimeOffset Expires);
public record RefreshTokenRedemption(LiveUser User, string ServiceTarget, Guid? SsoSessionId = null);
public record DeviceAuthToken(string SealedToken, byte[] ProofKey, DateTimeOffset Created, DateTimeOffset Expires);
public record AuthenticatedUser(LiveUser User, byte[]? SessionKey);

public class TokenManager(
    IConfiguration configuration,
    ILogger<TokenManager> logger,
    UserManager<LiveUser> userManager,
    LiveDbContext dbContext)
{
    private const string JwtIssuer = "https://relivewp.net/";

    private int RefreshTokenLifetimeDays =>
        int.TryParse(configuration["JWT:RefreshTokenLifetimeDays"], out var days) ? days : 90;

    public TimeSpan WebAccessTokenLifetime =>
        TimeSpan.FromMinutes(int.TryParse(configuration["JWT:WebAccessTokenLifetimeMinutes"], out var mins) ? mins : 60);

    public TimeSpan WebRefreshTokenLifetime =>
        TimeSpan.FromDays(int.TryParse(configuration["JWT:WebRefreshTokenLifetimeDays"], out var days) ? days : 14);

    private int DaTokenLifetimeDays =>
        int.TryParse(configuration["Passport:DaTokenLifetimeDays"], out var days) ? days : 30;

    private byte[] StsKey => Convert.FromBase64String(
        configuration["Passport:StsKey"] ?? throw new InvalidOperationException("Passport:StsKey is not configured."));

    public DeviceAuthToken IssueDeviceAuthToken(LiveUser user)
    {
        var sessionKey = DaToken.GenerateSessionKey();
        var created = DateTimeOffset.UtcNow;
        var expires = created.AddDays(DaTokenLifetimeDays);

        var sealed_ = DaToken.Seal(StsKey, new DaTokenPayload(
            Puid: user.Puid,
            Cid: user.Cid,
            MemberName: user.Email ?? user.UserName ?? "",
            SessionKey: sessionKey,
            Created: created,
            Expires: expires));

        return new DeviceAuthToken(sealed_, sessionKey, created, expires);
    }

    public DaTokenPayload UnsealDeviceAuthToken(string cipherValue)
        => DaToken.Unseal(StsKey, cipherValue);

    public SecurityToken IssueJwtAsync(LiveUser user, string serviceTarget, TimeSpan? lifetime = null, Guid? sessionId = null)
    {
        List<Claim> authClaims =
        [
            new Claim(JwtRegisteredClaimNames.Aud, serviceTarget),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, JwtIssuer),

            ..BuildIdentityClaims(user, sessionId)
        ];

        var created = DateTimeOffset.UtcNow;
        var expires = created.Add(lifetime ?? TimeSpan.FromDays(30));
        var token = CreateToken(authClaims, expires);

        return new SecurityToken(new JwtSecurityTokenHandler().WriteToken(token), created, expires);
    }

    public IEnumerable<Claim> BuildIdentityClaims(LiveUser user, Guid? sessionId = null)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            ..(sessionId is { } sid ? new[] { new Claim(JwtRegisteredClaimNames.Sid, sid.ToString()) } : []),
            new Claim("cid", user.Cid),
            new Claim("puid", user.Puid.ToString("X16")),
            new Claim("user_type", ((int)user.Type).ToString()),
            //new Claim(JwtRegisteredClaimNames.GivenName, "Thomas"),
            //new Claim(JwtRegisteredClaimNames.FamilyName, "May"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.PreferredUsername, user.UserName ?? ""),
        ];
    }

    public async Task<SecurityToken> IssueRefreshTokenAsync(LiveUser user, string serviceTarget, TimeSpan? lifetime = null, Guid? sessionId = null)
    {
        var raw = GenerateRawToken();
        var created = DateTimeOffset.UtcNow;
        var expires = created.Add(lifetime ?? TimeSpan.FromDays(RefreshTokenLifetimeDays));

        dbContext.LiveRefreshTokens.Add(new LiveRefreshToken()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(raw),
            ServiceTarget = serviceTarget,
            CreatedAt = created,
            ExpiresAt = expires,
            DeviceId = user.DeviceId,
            SsoSessionId = sessionId,
        });
        await dbContext.SaveChangesAsync();

        return new SecurityToken(raw, created, expires);
    }

    public async Task<RefreshTokenRedemption?> RedeemRefreshTokenAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var now = DateTimeOffset.UtcNow;

        var token = await dbContext.LiveRefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token == null || token.ExpiresAt <= now)
            return null;

        // rotation revokes on redeem, so a revoked token coming back means it was captured and
        // replayed. we can't tell the thief from the victim, so drop the whole family.
        if (token.RevokedAt != null)
        {
            logger.LogWarning("Refresh token {Id} replayed after rotation, revoking family for user {User}", token.Id, token.UserId);
            await RevokeRefreshTokenFamilyAsync(token, now);
            return null;
        }

        // the session is what actually authorised this chain, so a revoked or lapsed one stops
        // rotation even though the token row still looks fine
        if (token.SsoSessionId is { } session && !await IsSessionLiveAsync(session, now))
            return null;

        var affected = await dbContext.LiveRefreshTokens
            .Where(t => t.Id == token.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.LastUsedAt, now));
        if (affected == 0)
            return null;

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user == null)
            return null;

        return new RefreshTokenRedemption(user, token.ServiceTarget, token.SsoSessionId);
    }

    private async Task<bool> IsSessionLiveAsync(Guid sessionId, DateTimeOffset now)
    {
        var session = await dbContext.LiveSsoSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session != null && session.RevokedAt == null && session.AbsoluteExpiresAt > now;
    }

    private async Task RevokeRefreshTokenFamilyAsync(LiveRefreshToken token, DateTimeOffset now)
    {
        var family = dbContext.LiveRefreshTokens.Where(t => t.RevokedAt == null);

        family = token.SsoSessionId is { } sessionId
            ? family.Where(t => t.SsoSessionId == sessionId)
            : token.DeviceId is { } deviceId
                ? family.Where(t => t.DeviceId == deviceId)
                : family.Where(t => t.UserId == token.UserId && t.ServiceTarget == token.ServiceTarget);

        await family.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

        // the cookie outlives the tokens otherwise, and whoever holds it just mints a new family
        if (token.SsoSessionId is { } revoked)
        {
            await dbContext.LiveSsoSessions
                .Where(s => s.Id == revoked && s.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));
        }
    }

    public async Task<int> RevokeTokensForSessionAsync(Guid sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.LiveRefreshTokens
            .Where(t => t.SsoSessionId == sessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }

    public async Task RevokeTokensForDeviceAsync(string deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        await dbContext.LiveRefreshTokens
            .Where(t => t.DeviceId == deviceId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }

    private static string GenerateRawToken()
        => "rt_" + Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private JwtSecurityToken CreateToken(List<Claim> authClaims, DateTimeOffset expires)
    {
        var token = new JwtSecurityToken(
            expires: expires.UtcDateTime,
            claims: authClaims,
            signingCredentials: GetSigningCredentials()
        );

        return token;
    }

    public async Task<AuthenticatedUser?> GetUserForSecurityTokenAsync(SecurityTokensRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
        {
            var user = await userManager.FindByEmailAsync(request.Username);
            if (user == null)
            {
                user = await userManager.FindByNameAsync(request.Username);
                if (user == null)
                    return null;
            }

            if (!await userManager.CheckPasswordAsync(user, request.Password))
                return null;

            return new AuthenticatedUser(user, null);
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceAuthToken))
        {
            try
            {
                var payload = UnsealDeviceAuthToken(request.DeviceAuthToken);
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Puid == payload.Puid);
                return user is null ? null : new AuthenticatedUser(user, payload.SessionKey);
            }
            catch (DaTokenException ex)
            {
                logger.LogWarning(ex, "Presented DA token could not be unsealed");
                return null;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AuthToken))
        {
            TokenValidationResult result = await ValidateJwtAsync(request.AuthToken, ["http://Passport.NET/tb"]);

            if (!result.IsValid)
            {
                return null;
            }

            if (!result.Claims.TryGetValue(ClaimTypes.NameIdentifier, out var userId))
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId.ToString()!);
            return user is null ? null : new AuthenticatedUser(user, null);
        }

        return null;
    }

    public async Task<TokenValidationResult> ValidateJwtAsync(string token, string[] audiences)
    {
        var validationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtKeyLoader.GetVerifyingKey(configuration, logger),
            ValidAlgorithms = JwtKeyLoader.GetValidAlgorithms(configuration),

            ValidateIssuer = true,
            ValidIssuer = JwtIssuer,

            ValidateAudience = true,
            ValidAudiences = audiences,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // +- 5 mins is fine, these devices are Old
        };

        var handler = new JwtSecurityTokenHandler();
        var response = await handler.ValidateTokenAsync(token, validationParameters);
        if (!response.IsValid)
            logger.LogWarning(response.Exception, "Invalid token request?");

        return response;
    }

    private SigningCredentials GetSigningCredentials()
    {
        var type = configuration["JWT:SignatureAlgorithm"] ?? "SHA256-HMAC";
        if (type == "ES256")
        {
            var provider = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = Convert.FromBase64String(configuration["JWT:PrivateKey"]!);
            provider.ImportECPrivateKey(privateKey, out _);

            var signingKey = new ECDsaSecurityKey(provider);
            var signingAlgorithm = SecurityAlgorithms.EcdsaSha256;
            return new SigningCredentials(signingKey, signingAlgorithm);
        }
        else
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
            var signingAlgorithm = SecurityAlgorithms.HmacSha256;
            return new SigningCredentials(signingKey, signingAlgorithm);
        }
    }

}
