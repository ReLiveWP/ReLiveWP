namespace ReLiveWP.ProductKeys;

public static class ProductKeyLayout
{
    public const int UpgradeBits = 1;
    public const int SerialBits = 30;
    public const int HashBits = 28;
    public const int SignatureBits = 55;
    public const int TotalBits = UpgradeBits + SerialBits + HashBits + SignatureBits;

    private const int SerialShift = UpgradeBits;
    private const int HashShift = SerialShift + SerialBits;
    private const int SignatureShift = HashShift + HashBits;

    public const int HashMask = (1 << HashBits) - 1;
    public const long SignatureLimit = 1L << SignatureBits;

    public static UInt128 PackFields(ProductKeyFields fields)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fields.Serial);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fields.Serial, 1 << SerialBits);
        ArgumentOutOfRangeException.ThrowIfNegative(fields.Hash);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fields.Hash, HashMask);
        ArgumentOutOfRangeException.ThrowIfNegative(fields.Signature);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fields.Signature, SignatureLimit);

        var upgrade = fields.IsUpgrade ? UInt128.One : UInt128.Zero;
        var serial = (UInt128)(uint)fields.Serial << SerialShift;
        var hash = (UInt128)(uint)fields.Hash << HashShift;
        var signature = (UInt128)(ulong)fields.Signature << SignatureShift;

        return signature | hash | serial | upgrade;
    }

    public static ProductKeyFields UnpackFields(UInt128 value)
    {
        var isUpgrade = (value & UInt128.One) != UInt128.Zero;
        var serial = (int)(uint)(value >> SerialShift & ((UInt128.One << SerialBits) - 1));
        var hash = (int)(uint)(value >> HashShift & HashMask);
        var signature = (long)(ulong)(value >> SignatureShift & (ulong)(SignatureLimit - 1));

        return new ProductKeyFields(isUpgrade, serial, hash, signature);
    }
}
