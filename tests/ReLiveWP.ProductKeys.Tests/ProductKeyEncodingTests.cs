namespace ReLiveWP.ProductKeys.Tests;

public class ProductKeyEncodingTests
{
    [Fact]
    public void Base24RoundTrips()
    {
        var random = new Random(1234);
        Span<byte> buffer = stackalloc byte[16];

        for (var i = 0; i < 1000; i++)
        {
            random.NextBytes(buffer);
            var value = new UInt128(BitConverter.ToUInt64(buffer[8..]), BitConverter.ToUInt64(buffer)) % ProductKeyBase24.ValueLimit;

            var productKey = ProductKeyBase24.EncodeProductKey(value);

            Assert.Equal(29, productKey.Length);
            Assert.True(ProductKeyBase24.TryDecodeProductKey(productKey, out var decoded));
            Assert.Equal(value, decoded);
        }
    }

    [Fact]
    public void ZeroEncodesToAllFirstDigits()
    {
        Assert.Equal("BBBBB-BBBBB-BBBBB-BBBBB-BBBBB", ProductKeyBase24.EncodeProductKey(UInt128.Zero));
    }

    [Fact]
    public void DecodeIgnoresDashesAndCase()
    {
        var productKey = ProductKeyBase24.EncodeProductKey(123456789);
        var squashed = productKey.Replace("-", "").ToLowerInvariant();

        Assert.True(ProductKeyBase24.TryDecodeProductKey(squashed, out var decoded));
        Assert.Equal((UInt128)123456789, decoded);
    }

    [Theory]
    [InlineData("NOPVK-NOPVK-NOPVK-NOPVK-NOPVK")]
    [InlineData("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA")]
    [InlineData("BBBBB-BBBBB-BBBBB-BBBBB-BBBB")]
    [InlineData("BBBBB-BBBBB-BBBBB-BBBBB-BBBBBB")]
    [InlineData("BBBBB-BBBBB-BBBBB-BBBBB-BBBB1")]
    [InlineData("BBBBB-BBBBB BBBBB-BBBBB-BBBBB")]
    [InlineData("")]
    [InlineData("99999-99999-99999-99999-99999")]
    public void DecodeRejects(string productKey)
    {
        Assert.False(ProductKeyBase24.TryDecodeProductKey(productKey, out _));
    }

    [Fact]
    public void FieldsLandInTheXpBitPositions()
    {
        Assert.Equal(UInt128.One, ProductKeyLayout.PackFields(new ProductKeyFields(true, 0, 0, 0)));
        Assert.Equal(UInt128.One << 1, ProductKeyLayout.PackFields(new ProductKeyFields(false, 1, 0, 0)));
        Assert.Equal(UInt128.One << 31, ProductKeyLayout.PackFields(new ProductKeyFields(false, 0, 1, 0)));
        Assert.Equal(UInt128.One << 59, ProductKeyLayout.PackFields(new ProductKeyFields(false, 0, 0, 1)));
    }

    [Fact]
    public void LayoutRoundTrips()
    {
        var fields = new ProductKeyFields(true, ProductKeyFields.ComposeSerial(999, 999999), ProductKeyLayout.HashMask, ProductKeyLayout.SignatureLimit - 1);

        var packed = ProductKeyLayout.PackFields(fields);

        Assert.True(packed < ProductKeyBase24.ValueLimit);
        Assert.Equal(fields, ProductKeyLayout.UnpackFields(packed));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1000, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1_000_000)]
    public void ComposeSerialRejectsOutOfRange(int channelId, int sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductKeyFields.ComposeSerial(channelId, sequence));
    }
}
