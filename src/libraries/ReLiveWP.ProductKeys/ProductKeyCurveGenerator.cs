using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace ReLiveWP.ProductKeys;

// y^2 = x^3 + x over p = 3 mod 4 is supersingular, so #E = p + 1 and we get to pick its factors
public static class ProductKeyCurveGenerator
{
    public const int OrderBits = ProductKeyLayout.SignatureBits;

    private const int PrimeCertainty = 128;

    public static ProductKeySigningKey GenerateSigningKey(SecureRandom random)
    {
        var order = new BigInteger(OrderBits, PrimeCertainty, random);
        var (p, cofactor) = FindFieldPrime(order, random);

        var curve = new FpCurve(p, BigInteger.One, BigInteger.Zero, order, cofactor);
        var generator = FindGenerator(curve, p, order, cofactor, random);

        var privateKey = DrawScalar(order, random);
        var publicKey = generator.Multiply(privateKey).Negate().Normalize();

        var productKeyCurve = new ProductKeyCurve(p, BigInteger.One, BigInteger.Zero,
                                                  generator.AffineXCoord.ToBigInteger(), generator.AffineYCoord.ToBigInteger(),
                                                  publicKey.AffineXCoord.ToBigInteger(), publicKey.AffineYCoord.ToBigInteger());

        var signingKey = new ProductKeySigningKey(productKeyCurve, order, privateKey);
        signingKey.EnsureConsistent();
        return signingKey;
    }

    private static (BigInteger P, BigInteger Cofactor) FindFieldPrime(BigInteger order, SecureRandom random)
    {
        const int cofactorBits = ProductKeyCurve.FieldBits - OrderBits;

        while (true)
        {
            var cofactor = new BigInteger(cofactorBits, random)
                .SetBit(cofactorBits - 1)
                .ClearBit(0)
                .ClearBit(1);

            var p = cofactor.Multiply(order).Subtract(BigInteger.One);
            if (p.BitLength != ProductKeyCurve.FieldBits)
                continue;

            if (p.IsProbablePrime(PrimeCertainty))
                return (p, cofactor);
        }
    }

    private static ECPoint FindGenerator(ECCurve curve, BigInteger p, BigInteger order, BigInteger cofactor, SecureRandom random)
    {
        var squareRootExponent = p.Add(BigInteger.One).ShiftRight(2);

        while (true)
        {
            var x = DrawScalar(p, random);
            var rightHandSide = x.ModPow(BigInteger.Three, p).Add(x).Mod(p);
            var y = rightHandSide.ModPow(squareRootExponent, p);
            if (!y.Multiply(y).Mod(p).Equals(rightHandSide))
                continue;

            var randomPoint = curve.CreatePoint(x, y);
            var generator = randomPoint.Multiply(cofactor).Normalize();
            if (generator.IsInfinity)
                continue;

            if (!generator.Multiply(order).IsInfinity)
                throw new InvalidOperationException("Generator order check failed, the curve maths is wrong.");

            return generator;
        }
    }

    private static BigInteger DrawScalar(BigInteger limit, SecureRandom random)
    {
        while (true)
        {
            var candidate = new BigInteger(limit.BitLength, random);
            if (candidate.SignValue > 0 && candidate.CompareTo(limit) < 0)
                return candidate;
        }
    }
}
