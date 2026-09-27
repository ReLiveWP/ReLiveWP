using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace ReLiveWP.ProductKeys;

public sealed class ProductKeySigner
{
    private readonly ProductKeySigningKey _signingKey;
    private readonly SecureRandom _random = new();

    public ProductKeySigner(ProductKeySigningKey signingKey)
    {
        signingKey.EnsureConsistent();
        _signingKey = signingKey;
    }

    public string CreateProductKey(int channelId, int sequence, bool isUpgrade = false)
    {
        var serial = ProductKeyFields.ComposeSerial(channelId, sequence);
        var unsigned = new ProductKeyFields(isUpgrade, serial, 0, 0);

        var curve = _signingKey.Curve;
        var order = _signingKey.GeneratorOrder;

        while (true)
        {
            var nonce = DrawNonce(order);
            var noncePoint = curve.Generator.Multiply(nonce);
            var hash = ProductKeyHash.ComputeKeyHash(unsigned.PackedSerial, noncePoint);

            var signature = BigInteger.ValueOf(hash)
                .Multiply(_signingKey.PrivateKey)
                .Add(nonce)
                .Mod(order);

            if (signature.BitLength > ProductKeyLayout.SignatureBits)
                continue;

            var fields = unsigned with { Hash = hash, Signature = signature.LongValue };
            var packed = ProductKeyLayout.PackFields(fields);
            return ProductKeyBase24.EncodeProductKey(packed);
        }
    }

    private BigInteger DrawNonce(BigInteger order)
    {
        while (true)
        {
            var candidate = new BigInteger(order.BitLength, _random);
            if (candidate.SignValue > 0 && candidate.CompareTo(order) < 0)
                return candidate;
        }
    }
}
