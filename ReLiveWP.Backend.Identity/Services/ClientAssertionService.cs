using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Backend.Identity.Services
{
    public class ClientAssertionService(IJWKProvider jwkProvider) : IClientAssertionService
    {
        public async Task<string> CreateClientAssertionAsync(string clientId, string issuer)
        {
            var key = await jwkProvider.GetJWK("Key1");

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
