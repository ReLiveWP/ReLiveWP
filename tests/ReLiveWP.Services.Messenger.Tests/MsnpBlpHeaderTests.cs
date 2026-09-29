using System.Text;
using ReLiveWP.Services.Messenger.Msnp;
using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpBlpHeaderTests
{
    private static byte[] DataSdg(byte[] body)
    {
        var headers = SdgPayload("", channel: "PE", messageType: "Data");
        return [.. Encoding.ASCII.GetBytes(headers), .. body];
    }

    [Fact]
    public void ASynNakWithPeerInfoIsRead()
    {
        byte[] body =
        [
            0x1C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x12, 0x34,
            0x03, 0x04, 0x00, 0x00, 0x56, 0x78,
            0x01, 0x0A, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04,
            0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        Assert.True(MsnpBlpHeader.TryRead(DataSdg(body), out var header));

        Assert.True(header.IsSyn);
        Assert.True(header.RequestsAck);
        Assert.Equal(0x1234u, header.Sequence);
        Assert.Equal(0x5678u, header.Nak);
        Assert.Null(header.Ack);
        Assert.Equal("sn 4660 len 0 syn rak nak 22136", header.ToString());
    }

    [Fact]
    public void AChunkWithoutOptionsIsRead()
    {
        byte[] body = [0x08, 0x02, 0x28, 0x00, 0x7F, 0x00, 0x00, 0x01, .. new byte[0x2800], 0x00, 0x00, 0x00, 0x00];

        Assert.True(MsnpBlpHeader.TryRead(DataSdg(body), out var header));

        Assert.False(header.IsSyn);
        Assert.Equal((ushort)0x2800, header.PayloadLength);
        Assert.Equal(0x7F000001u, header.Sequence);
        Assert.Equal("sn 2130706433 len 10240 rak", header.ToString());
    }

    [Fact]
    public void ATruncatedBodyIsNotRead()
    {
        Assert.False(MsnpBlpHeader.TryRead(DataSdg([0x1C, 0x03, 0x00]), out _));
    }
}
