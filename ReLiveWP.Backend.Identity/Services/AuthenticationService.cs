using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Services
{
    public class AuthenticationService(IConfiguration configuration,
                        ILogger<AuthenticationService> logger,
                        UserManager<LiveUser> userManager,
                        RoleManager<LiveRole> roleManager) : Authentication.AuthenticationBase
    {
        private const string JwtIssuer = "https://relivewp.net/";

        private const uint S_OK = 0x0;
        private const uint PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD = 0x80048821;
        private const uint PPCRL_AUTHSTATE_E_UNAUTHENTICATED = 0x80048800;
        private const uint PPCRL_AUTHSTATE_E_EXPIRED = 0x80048801;
        private const uint PPCRL_E_SQM_INTERNET_SEC_INVALID_CERT = 0x80048428;
        private const uint ERROR_ALREADY_EXISTS = 0x800700B7;

        public override async Task<VerifyTokenResponse> VerifySecurityToken(VerifyTokenRequest request, ServerCallContext context)
        {
            var result = await ValidateJwtAsync(request.Token, [.. request.ServiceTargets]);
            if (!result.IsValid)
            {
                // TODO: figure out what was actually invalid
                return new VerifyTokenResponse() { Code = PPCRL_AUTHSTATE_E_EXPIRED };
            }

            var response = new VerifyTokenResponse() { Code = S_OK };
            foreach (var claim in result.Claims)
            {
                if (claim.Value is string value) // TODO: other types
                    response.Claims.Add(new ClaimMessage() { Type = claim.Key, Value = value });
            }

            return response;
        }

        public override async Task<SecurityTokensResponse> GetSecurityTokens(SecurityTokensRequest request, ServerCallContext context)
        {
            var user = await GetUserForSecurityTokenAsync(request);
            if (user == null)
            {
                return new SecurityTokensResponse() { Code = PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD };
            }

            string[] policies = ["LEGACY", "HBI_KEY", "MBI", "MBI_KEY"];

            var chars = user.Id.ToString();
            var bytes = user.Id.ToByteArray();
            var time_low = BitConverter.ToUInt32(bytes, 0);
            var node = BitConverter.ToUInt32(bytes, 12);

            var cid = chars[19..23] + chars[24..36];
            var puid = ((ulong)time_low << 32) | node;

            var response = new SecurityTokensResponse()
            {
                Code = S_OK,
                Cid = cid,
                Puid = puid,
                Username = user.UserName,
                EmailAddress = user.Email
            };

            foreach (var tokenRequest in request.Requests)
            {
                // TODO: there's a few different tokens that can be requested here inclduing x509 certificates, for now
                // we're just working with JWTs which are our stand in for a BinarySecurityToken (aka a blob)

                var authClaims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Aud, tokenRequest.ServiceTarget),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iss, JwtIssuer)
                };

                var created = DateTimeOffset.UtcNow;
                var expires = created.AddDays(30);
                var token = CreateToken(authClaims, expires);

                response.Tokens.Add(new SecurityTokenResponse()
                {
                    ServiceTarget = tokenRequest.ServiceTarget,
                    Created = Timestamp.FromDateTimeOffset(created),
                    Expires = Timestamp.FromDateTimeOffset(expires),
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    TokenType = "JWT",
                });
            }

            return response;
        }

        public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            if (await userManager.FindByEmailAsync(request.EmailAddress) != null)
                return new RegisterResponse() { Code = ERROR_ALREADY_EXISTS };

            var user = new LiveUser()
            {
                UserName = request.Username,
                Email = request.EmailAddress,
            };

            var result = await userManager.CreateAsync(user, request.Password);
        
            return new RegisterResponse() { Code = S_OK };
        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims, DateTimeOffset expires)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                expires: expires.UtcDateTime,
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        private string NormaliseUsername(string username)
        {
            if (username.IndexOf('@') != -1)
                return username[..username.IndexOf('@')];
            return username;
        }

        private async Task<TokenValidationResult> ValidateJwtAsync(string token, string[] audiences)
        {
            var key = Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!);

            // only Passport.NET tokens are valid for issuing other tokens
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

            return await handler.ValidateTokenAsync(token, validationParameters);
        }

        private async Task<LiveUser?> GetUserForSecurityTokenAsync(SecurityTokensRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            {
                var user = await userManager.FindByEmailAsync(request.Username);
                if (user == null)
                    return null;

                if (!await userManager.CheckPasswordAsync(user, request.Password))
                    return null;

                return user;
            }

            if (!string.IsNullOrWhiteSpace(request.AuthToken))
            {
                TokenValidationResult result = await ValidateJwtAsync(request.AuthToken, ["http://Passport.NET/tb"]);
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

                if (!result.Claims.TryGetValue(ClaimTypes.NameIdentifier, out var userId))
                {
                    return null;
                }

                return await userManager.FindByIdAsync(userId.ToString()!);
            }

            return null;
        }
    }
}