using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Identity.LiveID;

namespace ReLiveWP.Identity.Tests;

// Verifies the shared verify-key loader accepts tokens signed by the ES256 private key using only the
// distributed public key — the mechanism that lets every service validate JWTs locally.
public class JwtKeyLoaderTests
{
    private const string Audience = "relivewp.net";

    private static string Sign(ECDsa privateKey, string audience = Audience, TimeSpan? lifetime = null)
    {
        var credentials = new SigningCredentials(new ECDsaSecurityKey(privateKey), SecurityAlgorithms.EcdsaSha256);
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(30));
        var token = new JwtSecurityToken(
            issuer: JwtKeyLoader.Issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-123")],
            notBefore: expires.AddMinutes(-30),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IConfiguration PublicKeyConfig(ECDsa key) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:SignatureAlgorithm"] = "ES256",
            ["JWT:PublicKey"] = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
        }).Build();

    private static Task<TokenValidationResult> Validate(string token, IConfiguration config, string audience = Audience) =>
        new JwtSecurityTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtKeyLoader.GetVerifyingKey(config),
            ValidateIssuer = true,
            ValidIssuer = JwtKeyLoader.Issuer,
            ValidateAudience = true,
            ValidAudiences = [audience],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        });

    [Fact]
    public async Task Token_signed_with_private_key_verifies_with_public_key()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var result = await Validate(Sign(key), PublicKeyConfig(key));

        Assert.True(result.IsValid);
        Assert.Equal("user-123", result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public async Task Token_signed_with_different_key_fails()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var result = await Validate(Sign(signingKey), PublicKeyConfig(otherKey));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Token_for_wrong_audience_fails()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var result = await Validate(Sign(key, audience: "someone.else"), PublicKeyConfig(key));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Expired_token_fails()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // expired well beyond the 5-minute clock skew
        var result = await Validate(Sign(key, lifetime: TimeSpan.FromMinutes(-10)), PublicKeyConfig(key));

        Assert.False(result.IsValid);
    }
}
