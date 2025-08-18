using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Backend.Identity.Services
{
    public class ClientAssertionService(IConfiguration configuration) : IClientAssertionService
    {
        public string CreateClientAssertion(string clientId, string issuer)
        {
            var key = configuration["AtProtoOAuth:JWK"]
                    ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));

            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresAt = issuedAt + 300;

            var authClaims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Iss, clientId),
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim(JwtRegisteredClaimNames.Aud, issuer),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                expires: DateTimeOffset.Now.AddMinutes(5).UtcDateTime,
                claims: authClaims,
                signingCredentials: new SigningCredentials(new JsonWebKey(key) { KeyId = "Key1" }, SecurityAlgorithms.EcdsaSha256)
            );

            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(token);
            return tokenString;
        }
    }
}
