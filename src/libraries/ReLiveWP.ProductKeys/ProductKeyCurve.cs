using System.Globalization;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace ReLiveWP.ProductKeys;

public sealed class ProductKeyCurve
{
    public const int FieldBits = 384;
    public const int FieldBytes = FieldBits / 8;

    public BigInteger P { get; }
    public BigInteger A { get; }
    public BigInteger B { get; }

    public ECCurve Curve { get; }
    public ECPoint Generator { get; }
    public ECPoint PublicKey { get; }

    public ProductKeyCurve(BigInteger p, BigInteger a, BigInteger b,
                           BigInteger generatorX, BigInteger generatorY,
                           BigInteger publicKeyX, BigInteger publicKeyY)
    {
        if (p.BitLength > FieldBits)
            throw new ArgumentException($"p is {p.BitLength} bits, the key hash only has room for {FieldBits}.", nameof(p));

        P = p;
        A = a;
        B = b;

        Curve = new FpCurve(p, a, b, null, null);
        Generator = Curve.ValidatePoint(generatorX, generatorY);
        PublicKey = Curve.ValidatePoint(publicKeyX, publicKeyY);
    }

    public static ProductKeyCurve FromHex(string p, string a, string b,
                                          string generatorX, string generatorY,
                                          string publicKeyX, string publicKeyY)
    {
        return new ProductKeyCurve(ParseHex(p), ParseHex(a), ParseHex(b),
                                   ParseHex(generatorX), ParseHex(generatorY),
                                   ParseHex(publicKeyX), ParseHex(publicKeyY));
    }

    public static BigInteger ParseHex(string hex)
    {
        var digits = hex.Trim();
        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            digits = digits[2..];

        if (digits.Length == 0 || !digits.All(char.IsAsciiHexDigit))
            throw new FormatException($"'{hex}' is not a hex number.");

        return new BigInteger(digits, 16);
    }

    public static string FormatHex(BigInteger value) =>
        value.ToString(16).ToUpper(CultureInfo.InvariantCulture);
}
