using System.Security.Cryptography;
using System.Text;
using ReLiveWP.Services.Login.Services;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Tests;

// device side is LiveIDUtils.dll RsaPubKey::ParseFromJS and Credential::AppendEncryptedProperties
public class SignupCryptoTests
{
    private static readonly RSA Key = RSA.Create(2048);

    private record DevicePublicKey(string Ski, byte[] Exponent, byte[] Modulus);

    private static DevicePublicKey ParseScriptLikeDevice(string script)
    {
        string? ski = null;
        string? exponentHex = null;
        string? modulusHex = null;

        var position = 0;
        while (position < script.Length && (ski == null || exponentHex == null))
        {
            while (position < script.Length && script[position] is ' ' or '\t' or '\r' or '\n')
                position++;

            var nameStart = script.IndexOf(' ', position) + 1;
            var equals = script.IndexOf('=', nameStart);
            var name = script[nameStart..equals].TrimEnd(' ');
            var valueStart = script.IndexOf('"', equals + 1) + 1;
            var valueEnd = script.IndexOf('"', valueStart);
            var terminator = script.IndexOf(';', valueEnd + 1);
            Assert.True(terminator > valueEnd, "statement must end with ;");

            var value = script[valueStart..valueEnd];
            if (name.Equals("key", StringComparison.OrdinalIgnoreCase))
            {
                var exponentStart = value.IndexOf('=') + 1;
                var exponentEnd = value.IndexOf(';', exponentStart);
                exponentHex = value[exponentStart..exponentEnd];
                modulusHex = value[(value.IndexOf('=', exponentEnd) + 1)..];
            }
            else if (name.Equals("ski", StringComparison.OrdinalIgnoreCase))
            {
                ski = value;
            }

            position = terminator + 1;
        }

        Assert.NotNull(ski);
        Assert.NotNull(exponentHex);
        return new DevicePublicKey(ski, Convert.FromHexString(exponentHex), Convert.FromHexString(modulusHex!));
    }

    private static string EncryptLikeDevice(RSAParameters publicKey, string password)
    {
        var passwordBytes = Encoding.Latin1.GetBytes(password);
        var blob = new byte[passwordBytes.Length + 6];
        blob[0] = 0x01;
        blob[1] = 0x02;
        blob[2] = (byte)passwordBytes.Length;
        passwordBytes.CopyTo(blob, 3);

        using var rsa = RSA.Create(publicKey);
        var ciphertext = rsa.Encrypt(blob, RSAEncryptionPadding.OaepSHA1);
        Array.Reverse(ciphertext);
        return Convert.ToBase64String(ciphertext);
    }

    [Fact]
    public void Public_key_script_reads_back_the_way_the_device_parses_it()
    {
        var ski = SignupKeyRing.ComputeSki(Key);
        var script = SignupCrypto.FormatPublicKeyScript(ski, Key);

        var parsed = ParseScriptLikeDevice(script);

        var expected = Key.ExportParameters(false);
        Assert.Equal(ski, parsed.Ski);
        Assert.Equal(expected.Exponent, parsed.Exponent);
        Assert.Equal(expected.Modulus, parsed.Modulus);
    }

    [Fact]
    public void Password_encrypted_like_the_device_decrypts()
    {
        var script = SignupCrypto.FormatPublicKeyScript(SignupKeyRing.ComputeSki(Key), Key);
        var parsed = ParseScriptLikeDevice(script);
        var cipherValue = EncryptLikeDevice(new RSAParameters() { Exponent = parsed.Exponent, Modulus = parsed.Modulus }, "hunter22!");

        var password = SignupCrypto.DecryptPassword(Key, cipherValue);

        Assert.Equal("hunter22!", password);
    }

    [Fact]
    public void Password_for_another_key_does_not_decrypt()
    {
        using var otherKey = RSA.Create(2048);
        var cipherValue = EncryptLikeDevice(otherKey.ExportParameters(false), "hunter22");

        Assert.Null(SignupCrypto.DecryptPassword(Key, cipherValue));
    }

    [Fact]
    public void Garbage_cipher_value_does_not_decrypt()
    {
        Assert.Null(SignupCrypto.DecryptPassword(Key, "not base64 at all!"));
    }

    [Theory]
    [InlineData(new byte[] { 0x02, 0x02, 0x01, 0x41, 0, 0, 0 })]
    [InlineData(new byte[] { 0x01, 0x01, 0x01, 0x41, 0, 0, 0 })]
    [InlineData(new byte[] { 0x01, 0x02, 0x09, 0x41, 0, 0, 0 })]
    [InlineData(new byte[] { 0x01, 0x02, 0x00, 0, 0, 0 })]
    [InlineData(new byte[] { 0x01, 0x02 })]
    public void Malformed_password_blobs_are_refused(byte[] blob)
    {
        Assert.Null(SignupCrypto.ReadPasswordBlob(blob));
    }

    [Fact]
    public void Ski_is_stable_for_a_key()
    {
        Assert.Equal(SignupKeyRing.ComputeSki(Key), SignupKeyRing.ComputeSki(Key));
        Assert.Equal(40, SignupKeyRing.ComputeSki(Key).Length);
    }
}
