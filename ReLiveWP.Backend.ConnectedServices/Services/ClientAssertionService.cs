using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class ClientAssertionService(IJWKProvider jwkProvider, ILogger<ClientAssertionService> logger) : IClientAssertionService
{
    public async Task<string> CreateClientAssertionAsync(string clientId, string issuer, string keyId)
    {
        var key = await jwkProvider.GetJWKAsync(keyId);
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var jKey = new JsonWebKey(key);
        logger.LogInformation("Using key {KeyId}", jKey.KeyId);

        var authClaims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: clientId,
            audience: issuer,
            expires: DateTimeOffset.Now.AddMinutes(5).UtcDateTime,
            claims: authClaims,
            signingCredentials: new SigningCredentials(jKey, SecurityAlgorithms.EcdsaSha256)
        );

        var handler = new JwtSecurityTokenHandler();
        var written = handler.WriteToken(token);
        var decoded = handler.ReadJwtToken(written);

        return handler.WriteToken(token);
    }
}
