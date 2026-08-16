using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

public class WbxmlMultiByteIntegerTests
{
    [Theory]
    [InlineData(0x00, new byte[] { 0x00 })]
    [InlineData(0x60, new byte[] { 0x60 })]
    [InlineData(0x7F, new byte[] { 0x7F })]
    [InlineData(0x80, new byte[] { 0x81, 0x00 })]
    [InlineData(0xA0, new byte[] { 0x81, 0x20 })]
    [InlineData(200, new byte[] { 0x81, 0x48 })]
    [InlineData(0x3FFF, new byte[] { 0xFF, 0x7F })]
    [InlineData(0x4000, new byte[] { 0x81, 0x80, 0x00 })]
    public void Encodes_mb_u_int32(int value, byte[] expected)
    {
        Assert.Equal(expected, ASWBXML.EncodeMultiByteInteger(value));
    }
}
