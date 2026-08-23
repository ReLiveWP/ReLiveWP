using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Identity.LiveID;

public static class JwtKeyLoader
{
    public const string Issuer = "https://relivewp.net/";

    // pinned so a service that loses its JWT:SignatureAlgorithm cannot silently start accepting a
    // different family of signature than the one Identity actually issues
    public static string[] GetValidAlgorithms(IConfiguration configuration)
        => (configuration["JWT:SignatureAlgorithm"] ?? "SHA256-HMAC") == "ES256"
            ? [SecurityAlgorithms.EcdsaSha256]
            : [SecurityAlgorithms.HmacSha256];

    public static SecurityKey GetVerifyingKey(IConfiguration configuration, ILogger? logger = null)
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
                provider.ImportECPrivateKey(Convert.FromBase64String(configuration["JWT:PrivateKey"]!), out _);
                logger?.LogWarning("JWT:PublicKey is not configured; derived from private key. Set JWT:PublicKey to: {PublicKey}",
                    Convert.ToBase64String(provider.ExportSubjectPublicKeyInfo()));
            }

            return new ECDsaSecurityKey(provider);
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
    }
}
