using System.Text;

namespace ReLiveWP.ProductKeys;

public static class ProductKeyBase24
{
    public const string Alphabet = "BCDFGHJKMPQRTVWXY2346789";
    public const int DigitCount = 25;
    public const int GroupLength = 5;

    public static readonly UInt128 ValueLimit = UInt128.One << ProductKeyLayout.TotalBits;

    public static bool TryDecodeProductKey(string productKey, out UInt128 value)
    {
        value = UInt128.Zero;

        var digits = productKey.Replace("-", "").ToUpperInvariant();
        if (digits.Length != DigitCount)
            return false;

        foreach (var character in digits)
        {
            var digit = Alphabet.IndexOf(character);
            if (digit < 0)
                return false;

            value = value * (uint)Alphabet.Length + (uint)digit;
        }

        return value < ValueLimit;
    }

    public static string EncodeProductKey(UInt128 value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, ValueLimit);

        var digits = new char[DigitCount];
        for (var i = DigitCount - 1; i >= 0; i--)
        {
            digits[i] = Alphabet[(int)(value % (uint)Alphabet.Length)];
            value /= (uint)Alphabet.Length;
        }

        var builder = new StringBuilder(DigitCount + DigitCount / GroupLength - 1);
        for (var i = 0; i < DigitCount; i += GroupLength)
        {
            if (i > 0)
                builder.Append('-');
            builder.Append(digits, i, GroupLength);
        }

        return builder.ToString();
    }
}
