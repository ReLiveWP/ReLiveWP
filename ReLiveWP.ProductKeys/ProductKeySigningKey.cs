using Org.BouncyCastle.Math;

namespace ReLiveWP.ProductKeys;

public sealed record ProductKeySigningKey(ProductKeyCurve Curve, BigInteger GeneratorOrder, BigInteger PrivateKey)
{
    public void EnsureConsistent()
    {
        if (!Curve.Generator.Multiply(GeneratorOrder).IsInfinity)
            throw new InvalidOperationException("GeneratorOrder is not the order of the generator.");

        var expectedPublicKey = Curve.Generator.Multiply(PrivateKey).Negate().Normalize();
        if (!expectedPublicKey.Equals(Curve.PublicKey))
            throw new InvalidOperationException("PublicKey is not -(PrivateKey * Generator).");
    }
}
