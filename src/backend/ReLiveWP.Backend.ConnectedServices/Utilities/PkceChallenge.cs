using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Backend.ConnectedServices.Utilities;

public static class PkceChallenge
{
    public static string CreateS256(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncoder.Encode(hash);
    }
}
