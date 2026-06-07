using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Services;

public record struct SecurityToken(string Token, DateTimeOffset Created, DateTimeOffset Expires);

public class TokenManager(
    IConfiguration configuration,
    ILogger<TokenManager> logger,
    UserManager<LiveUser> userManager)
{
    private const string JwtIssuer = "https://relivewp.net/";

    private readonly SymmetricSecurityKey signingKey
         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));

    public async Task<SecurityToken> CreateJwtSecurityToken(LiveUser user, string serviceTarget)
    {
        var authClaims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Aud, serviceTarget),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, JwtIssuer),

            new Claim("cid", user.Cid),
            new Claim("puid", Convert.ToHexString(BitConverter.GetBytes(user.Puid))),
            new Claim("user_type", ((int)user.Type).ToString()),
            //new Claim(JwtRegisteredClaimNames.GivenName, "Thomas"),
            //new Claim(JwtRegisteredClaimNames.FamilyName, "May"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.PreferredUsername, user.UserName ?? ""),
        };

        var created = DateTimeOffset.UtcNow;
        var expires = created.AddDays(30);
        var token = CreateToken(authClaims, expires);

        return new SecurityToken(new JwtSecurityTokenHandler().WriteToken(token), created, expires);
    }

    private JwtSecurityToken CreateToken(List<Claim> authClaims, DateTimeOffset expires)
    {
        var token = new JwtSecurityToken(
            expires: expires.UtcDateTime,
            claims: authClaims,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }

    // TODO: clean up all of this, it is bad.
    public async Task<LiveUser?> GetUserForSecurityTokenAsync(SecurityTokensRequest request)
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

            return user;
        }

        if (!string.IsNullOrWhiteSpace(request.AuthToken))
        {
            TokenValidationResult result = await ValidateJwtAsync(request.AuthToken, ["http://Passport.NET/tb"]);
#if !DEBUG
            if (!result.IsValid)
            {
                if (request.Requests.All(s => s.ServiceTarget == "commerce.zune.net"))
                {
                    // xbox/zune tokens are allowed to issue tokens for other specific targets
                    result = await ValidateJwtAsync(request.AuthToken, ["zune.live.net", "xbox.live.net", "kdc.xboxlive.com"]);
                }
            }

            if (!result.IsValid)
            {
                return null;
            }
#endif
            if (!result.Claims.TryGetValue(ClaimTypes.NameIdentifier, out var userId))
            {
                return null;
            }

            return await userManager.FindByIdAsync(userId.ToString()!);
        }

        return null;
    }

    public async Task<TokenValidationResult> ValidateJwtAsync(string token, string[] audiences)
    {
        var key = Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!);
        var validationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

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
}
