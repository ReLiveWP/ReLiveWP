using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace ReLiveWP.ProductKeys.Tests;

public class GeneratedCurveTests
{
    private static readonly Lazy<ProductKeySigningKey> SharedSigningKey =
        new(() => ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom()));

    [Fact]
    public void GeneratedCurveHasTheExpectedShape()
    {
        var signingKey = SharedSigningKey.Value;

        Assert.Equal(ProductKeyCurve.FieldBits, signingKey.Curve.P.BitLength);
        Assert.Equal(3, signingKey.Curve.P.IntValue & 3);
        Assert.Equal(ProductKeyCurveGenerator.OrderBits, signingKey.GeneratorOrder.BitLength);
        Assert.True(signingKey.GeneratorOrder.IsProbablePrime(64));

        signingKey.EnsureConsistent();
    }

    [Fact]
    public void SignedKeysVerify()
    {
        var signingKey = SharedSigningKey.Value;
        var signer = new ProductKeySigner(signingKey);
        var verifier = new ProductKeyVerifier(signingKey.Curve);

        for (var sequence = 0; sequence < 50; sequence++)
        {
            var productKey = signer.CreateProductKey(1, sequence);

            Assert.True(verifier.TryVerifyProductKey(productKey, out var fields), productKey);
            Assert.Equal(ProductKeyFields.ComposeSerial(1, sequence), fields.Serial);
        }
    }

    [Fact]
    public void KeysFromAnotherCurveDoNotVerify()
    {
        var otherSigningKey = ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom());
        var otherSigner = new ProductKeySigner(otherSigningKey);
        var verifier = new ProductKeyVerifier(SharedSigningKey.Value.Curve);

        for (var sequence = 0; sequence < 20; sequence++)
        {
            var productKey = otherSigner.CreateProductKey(1, sequence);
            Assert.False(verifier.TryVerifyProductKey(productKey, out _), productKey);
        }
    }

    [Fact]
    public void FlippingAnyBitBreaksTheKey()
    {
        var signingKey = SharedSigningKey.Value;
        var signer = new ProductKeySigner(signingKey);
        var verifier = new ProductKeyVerifier(signingKey.Curve);

        var productKey = signer.CreateProductKey(42, 4242);
        Assert.True(ProductKeyBase24.TryDecodeProductKey(productKey, out var value));

        for (var bit = 0; bit < ProductKeyLayout.TotalBits; bit++)
        {
            var tampered = value ^ (UInt128.One << bit);
            var tamperedKey = ProductKeyBase24.EncodeProductKey(tampered);

            Assert.False(verifier.TryVerifyProductKey(tamperedKey, out _), $"bit {bit}: {tamperedKey}");
        }
    }

    [Fact]
    public void SignerRejectsMismatchedPrivateKey()
    {
        var signingKey = SharedSigningKey.Value;
        var wrongKey = signingKey with { PrivateKey = signingKey.PrivateKey.Add(BigInteger.One) };

        Assert.Throws<InvalidOperationException>(() => new ProductKeySigner(wrongKey));
    }
}
