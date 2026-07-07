using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Passport;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Services;

public record struct SecurityToken(string Token, DateTimeOffset Created, DateTimeOffset Expires);
public record RefreshTokenRedemption(LiveUser User, string ServiceTarget);
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

    public SecurityToken IssueJwtAsync(LiveUser user, string serviceTarget)
    {
        List<Claim> authClaims =
        [
            new Claim(JwtRegisteredClaimNames.Aud, serviceTarget),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, JwtIssuer),

            ..BuildIdentityClaims(user)
        ];

        var created = DateTimeOffset.UtcNow;
        var expires = created.AddDays(30);
        var token = CreateToken(authClaims, expires);

        return new SecurityToken(new JwtSecurityTokenHandler().WriteToken(token), created, expires);
    }

    public IEnumerable<Claim> BuildIdentityClaims(LiveUser user)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("cid", user.Cid),
            new Claim("puid", user.Puid.ToString("X2").PadLeft(16, '0')),
            new Claim("user_type", ((int)user.Type).ToString()),
            //new Claim(JwtRegisteredClaimNames.GivenName, "Thomas"),
            //new Claim(JwtRegisteredClaimNames.FamilyName, "May"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.PreferredUsername, user.UserName ?? ""),
        ];
    }

    public async Task<SecurityToken> IssueRefreshTokenAsync(LiveUser user, string serviceTarget)
    {
        var raw = GenerateRawToken();
        var created = DateTimeOffset.UtcNow;
        var expires = created.AddDays(RefreshTokenLifetimeDays);

        dbContext.LiveRefreshTokens.Add(new LiveRefreshToken()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(raw),
            ServiceTarget = serviceTarget,
            CreatedAt = created,
            ExpiresAt = expires,
            DeviceId = user.DeviceId,
        });
        await dbContext.SaveChangesAsync();

        return new SecurityToken(raw, created, expires);
    }

    public async Task<RefreshTokenRedemption?> RedeemRefreshTokenAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var now = DateTimeOffset.UtcNow;

        var token = await dbContext.LiveRefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token == null || token.RevokedAt != null || token.ExpiresAt <= now)
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

        return new RefreshTokenRedemption(user, token.ServiceTarget);
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
            IssuerSigningKey = GetVerifyingCredentials(configuration),

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

    public static SecurityKey GetVerifyingCredentials(IConfiguration configuration)
    {
        var type = configuration["JWT:SignatureAlgorithm"] ?? "SHA256-HMAC";
        if (type == "ES256")
        {
            var provider = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKeyB64 = configuration["JWT:PublicKey"];
            if (publicKeyB64 != null)
            {
                provider.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyB64), out _);
            }
            else
            {
                // derive public key from the private key when no explicit public key is configured
                provider.ImportECPrivateKey(Convert.FromBase64String(configuration["JWT:PrivateKey"]!), out _);
            }

            return new ECDsaSecurityKey(provider);
        }
        else
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
        }
    }
}
