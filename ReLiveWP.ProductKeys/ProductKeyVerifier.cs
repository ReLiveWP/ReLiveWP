using System.Diagnostics.CodeAnalysis;
using Org.BouncyCastle.Math;

namespace ReLiveWP.ProductKeys;

public sealed class ProductKeyVerifier(ProductKeyCurve curve)
{
    public bool TryVerifyProductKey(string productKey, [NotNullWhen(true)] out ProductKeyFields? fields)
    {
        fields = null;

        if (!ProductKeyBase24.TryDecodeProductKey(productKey, out var value))
            return false;

        var decoded = ProductKeyLayout.UnpackFields(value);

        var signature = BigInteger.ValueOf(decoded.Signature);
        var hash = BigInteger.ValueOf(decoded.Hash);

        var signedPoint = curve.Generator.Multiply(signature);
        var hashedPoint = curve.PublicKey.Multiply(hash);
        var noncePoint = signedPoint.Add(hashedPoint).Normalize();
        if (noncePoint.IsInfinity)
            return false;

        var computedHash = ProductKeyHash.ComputeKeyHash(decoded.PackedSerial, noncePoint);
        if (computedHash != decoded.Hash)
            return false;

        fields = decoded;
        return true;
    }
}
