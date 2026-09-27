using System.Buffers.Binary;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace ReLiveWP.ProductKeys;

public static class ProductKeyHash
{
    private const int MessageLength = sizeof(int) + 2 * ProductKeyCurve.FieldBytes;

    public static int ComputeKeyHash(int packedSerial, ECPoint point)
    {
        var affinePoint = point.Normalize();

        Span<byte> message = stackalloc byte[MessageLength];
        BinaryPrimitives.WriteInt32LittleEndian(message, packedSerial);
        WriteFieldLittleEndian(affinePoint.AffineXCoord.ToBigInteger(), message.Slice(sizeof(int), ProductKeyCurve.FieldBytes));
        WriteFieldLittleEndian(affinePoint.AffineYCoord.ToBigInteger(), message.Slice(sizeof(int) + ProductKeyCurve.FieldBytes, ProductKeyCurve.FieldBytes));

        Span<byte> digest = stackalloc byte[SHA1.HashSizeInBytes];
        SHA1.HashData(message, digest);

        var leadingWord = BinaryPrimitives.ReadUInt32LittleEndian(digest);
        return (int)(leadingWord >> 4) & ProductKeyLayout.HashMask;
    }

    private static void WriteFieldLittleEndian(BigInteger value, Span<byte> destination)
    {
        var bigEndian = value.ToByteArrayUnsigned();
        destination.Clear();
        for (var i = 0; i < bigEndian.Length; i++)
            destination[i] = bigEndian[bigEndian.Length - 1 - i];
    }
}
