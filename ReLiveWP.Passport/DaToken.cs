using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReLiveWP.Passport;

public sealed record DaTokenPayload(
    [property: JsonPropertyName("puid")] long Puid,
    [property: JsonPropertyName("cid")] string Cid,
    [property: JsonPropertyName("member")] string MemberName,
    [property: JsonPropertyName("sk")] byte[] SessionKey,
    [property: JsonPropertyName("iat")] DateTimeOffset Created,
    [property: JsonPropertyName("exp")] DateTimeOffset Expires);

public sealed class DaTokenException(string message)
    : Exception(message);

public static class DaToken
{
    private const int IvLength = 8;         // 3DES block size
    public const int SessionKeyLength = 24; // 192-bit

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static byte[] GenerateSessionKey()
        => RandomNumberGenerator.GetBytes(SessionKeyLength);

    public static string Seal(byte[] stsKey, DaTokenPayload payload)
    {
        ArgumentNullException.ThrowIfNull(stsKey);
        ArgumentNullException.ThrowIfNull(payload);

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

        using var des = CreateCipher(stsKey);
        des.GenerateIV();
        using var encryptor = des.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var sealed_ = new byte[IvLength + ciphertext.Length];
        Buffer.BlockCopy(des.IV, 0, sealed_, 0, IvLength);
        Buffer.BlockCopy(ciphertext, 0, sealed_, IvLength, ciphertext.Length);
        return Convert.ToBase64String(sealed_);
    }

    public static DaTokenPayload Unseal(byte[] stsKey, string cipherValue, bool validateLifetime = true)
    {
        ArgumentNullException.ThrowIfNull(stsKey);
        ArgumentException.ThrowIfNullOrEmpty(cipherValue);

        byte[] sealed_;
        try { sealed_ = Convert.FromBase64String(cipherValue); }
        catch (FormatException) { throw new DaTokenException("DA token is not valid base64."); }

        if (sealed_.Length <= IvLength)
            throw new DaTokenException("DA token is too short.");

        var iv = sealed_[..IvLength];
        var ciphertext = sealed_[IvLength..];

        DaTokenPayload? payload;
        try
        {
            using var des = CreateCipher(stsKey);
            des.IV = iv;
            using var decryptor = des.CreateDecryptor();
            var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            payload = JsonSerializer.Deserialize<DaTokenPayload>(plaintext, JsonOptions);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            throw new DaTokenException("DA token could not be unsealed.");
        }

        if (payload is null)
            throw new DaTokenException("DA token payload was empty.");

        if (validateLifetime && payload.Expires <= DateTimeOffset.UtcNow)
            throw new DaTokenException("DA token has expired.");

        return payload;
    }

    private static TripleDES CreateCipher(byte[] stsKey)
    {
        var des = TripleDES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        des.Key = stsKey; // throws CryptographicException if not 16/24 bytes or a weak/degenerate key
        return des;
    }
}
