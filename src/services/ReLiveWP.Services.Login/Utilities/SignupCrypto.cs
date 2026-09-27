using System.Security.Cryptography;
using System.Text;

namespace ReLiveWP.Services.Login.Utilities;

public static class SignupCrypto
{
    private const byte PasswordBlobTag = 0x01;
    private const byte PasswordBlobVersion = 0x02;

    public static string FormatPublicKeyScript(string ski, RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        var exponent = Convert.ToHexString(parameters.Exponent!);
        var modulus = Convert.ToHexString(parameters.Modulus!);

        return $"var ski=\"{ski}\";var key=\"e={exponent};m={modulus}\";";
    }

    public static string? DecryptPassword(RSA rsa, string cipherValue)
    {
        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(cipherValue);
        }
        catch (FormatException)
        {
            return null;
        }

        Array.Reverse(ciphertext);

        byte[] blob;
        try
        {
            blob = rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1);
        }
        catch (CryptographicException)
        {
            return null;
        }

        return ReadPasswordBlob(blob);
    }

    public static string? ReadPasswordBlob(ReadOnlySpan<byte> blob)
    {
        if (blob.Length < 3 || blob[0] != PasswordBlobTag || blob[1] != PasswordBlobVersion)
            return null;

        var length = blob[2];
        if (length == 0 || blob.Length < 3 + length)
            return null;

        return Encoding.Latin1.GetString(blob.Slice(3, length));
    }
}
