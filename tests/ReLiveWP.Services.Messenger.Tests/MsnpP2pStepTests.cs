using System.Text;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpP2pStepTests
{
    private static byte[] WithBinaryHeader(string slp) =>
        [0x08, 0x00, 0x01, 0x2C, 0x0A, 0x00, 0x00, 0x01, .. Encoding.ASCII.GetBytes(slp)];

    [Fact]
    public void AnInviteIsNamedWithItsBody()
    {
        var payload = WithBinaryHeader(
            "INVITE MSNMSGR:bob@example.com;{AAAAAAAA-2222-3333-4444-555555555555} MSNSLP/1.0\r\n" +
            "To: <msnmsgr:bob@example.com>\r\n" +
            "Content-Type: application/x-msnmsgr-sessionreqbody\r\n" +
            "\r\n");

        var step = MsnpP2pStep.Describe(payload);

        Assert.Equal(new MsnpP2pStep("INVITE", "sessionreqbody"), step);
        Assert.Equal("INVITE sessionreqbody", step.ToString());
    }

    [Theory]
    [InlineData("MSNSLP/1.0 200 OK\r\nContent-Type: application/x-msnmsgr-sessionreqbody\r\n\r\n", "200", "sessionreqbody")]
    [InlineData("MSNSLP/1.0 603 Decline\r\n\r\n", "603", null)]
    [InlineData("BYE MSNMSGR:bob@example.com MSNSLP/1.0\r\nContent-Type: application/x-msnmsgr-sessionclosebody\r\n\r\n", "BYE", "sessionclosebody")]
    public void ResponsesAndByesAreNamed(string slp, string expectedStep, string? expectedBody)
    {
        Assert.Equal(new MsnpP2pStep(expectedStep, expectedBody), MsnpP2pStep.Describe(WithBinaryHeader(slp)));
    }

    [Fact]
    public void BinaryChunksAreData()
    {
        byte[] payload = [0x08, 0x00, 0x29, 0x0A, 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        Assert.Equal(new MsnpP2pStep(MsnpP2pStep.DataStep, null), MsnpP2pStep.Describe(payload));
    }
}
