using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Identity.LiveID;

public static class JwtKeyLoader
{
    public const string Issuer = "https://relivewp.net/";

    // Loads the key used to VERIFY tokens. For ES256 this is the public key (JWT:PublicKey, base64
    // SubjectPublicKeyInfo) — not secret, so it can be distributed as ordinary config. Falls back to
    // deriving the public key from JWT:PrivateKey when no explicit public key is present (issuer side).
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
                // No public key configured — surface the derived SPKI so it can be copied into JWT:PublicKey
                // and distributed to verifiers (they never see the private key).
                logger?.LogWarning("JWT:PublicKey is not configured; derived from private key. Set JWT:PublicKey to: {PublicKey}",
                    Convert.ToBase64String(provider.ExportSubjectPublicKeyInfo()));
            }

            return new ECDsaSecurityKey(provider);
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
    }
}
