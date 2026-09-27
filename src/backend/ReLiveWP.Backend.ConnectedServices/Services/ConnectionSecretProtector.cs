using System.Security.Cryptography;
using System.Text;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class ConnectionSecretProtector
{
    public const string KeyConfigPath = "ConnectedServices:Secrets:ConnectionKey";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[]? key;

    public ConnectionSecretProtector(IConfiguration configuration)
    {
        var configured = configuration[KeyConfigPath];
        if (string.IsNullOrWhiteSpace(configured))
            return;

        if (!TryDecodeKey(configured, out var decoded))
            throw new InvalidOperationException(
                $"{KeyConfigPath} must be {KeySize} bytes, base64 or hex encoded.");

        key = decoded;
    }

    public bool IsConfigured => key != null;

    public string Protect(string plaintext)
    {
        if (key == null)
            throw new InvalidOperationException($"{KeyConfigPath} is not configured.");

        var plain = Encoding.UTF8.GetBytes(plaintext);
        var payload = new byte[NonceSize + TagSize + plain.Length];

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (key == null)
            throw new InvalidOperationException($"{KeyConfigPath} is not configured.");

        var payload = Convert.FromBase64String(protectedValue);
        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Protected value is truncated.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private static bool TryDecodeKey(string configured, out byte[] decoded)
    {
        decoded = [];

        var trimmed = configured.Trim();

        if (trimmed.Length == KeySize * 2 &&
            trimmed.All(Uri.IsHexDigit))
        {
            decoded = Convert.FromHexString(trimmed);
            return true;
        }

        try
        {
            var bytes = Convert.FromBase64String(trimmed);
            if (bytes.Length != KeySize)
                return false;

            decoded = bytes;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
