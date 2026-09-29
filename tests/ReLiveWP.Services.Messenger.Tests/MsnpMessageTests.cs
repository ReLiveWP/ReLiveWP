using System.Text;
using ReLiveWP.Services.Messenger.Msnp;
using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpMessageTests
{
    private static readonly string AccentedHello = "h" + (char)0xE9 + "llo";

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();

    [Fact]
    public void ParsesTheSignInBatch()
    {
        var message = Parse(
            $"USR 4 SSO S t=ticket p= secret {Epid}\r\n" +
            "FSL 5 579 0\r\n" +
            "CHL 6\r\n" +
            SelfPresencePut("7"));

        Assert.Equal(["USR", "FSL", "CHL", "PUT"], message.Commands.Select(c => c.Verb));
        Assert.Equal(["4", "5", "6", "7"], message.Commands.Select(c => c.TrId));

        var put = message.Commands[3];
        Assert.Empty(put.Arguments);
        Assert.Equal(PresencePayload, put.PayloadText);
    }

    [Fact]
    public void PayloadLengthCountsBytesNotCharacters()
    {
        var message = Parse("SDG 9 6\r\n" + AccentedHello + "OUT\r\n");

        Assert.Equal(2, message.Commands.Count);
        Assert.Equal(AccentedHello, message.Commands[0].PayloadText);
        Assert.Equal("OUT", message.Commands[1].Verb);
    }

    [Fact]
    public void BinaryPayloadSurvivesIntact()
    {
        byte[] payload = [0x00, 0x0D, 0x0A, (byte)'O', (byte)'U', (byte)'T', 0x0D, 0x0A, 0xFF];
        var body = Concat(Encoding.ASCII.GetBytes($"SDG 10 {payload.Length}\r\n"), payload, "OUT\r\n"u8.ToArray());

        var message = Parse(body);

        Assert.Equal(2, message.Commands.Count);
        Assert.Equal(payload, message.Commands[0].Payload);
        Assert.Equal("OUT", message.Commands[1].Verb);
    }

    [Fact]
    public void QryCarriesItsHashAsPayload()
    {
        var hash = new string('a', 32);
        var message = Parse($"QRY 8 APPID 32\r\n{hash}");

        var qry = Assert.Single(message.Commands);
        Assert.Equal("8", qry.TrId);
        Assert.Equal(["APPID"], qry.Arguments);
        Assert.Equal(hash, qry.PayloadText);
    }

    [Fact]
    public void BareOutHasNoTrId()
    {
        var message = Parse("OUT\r\n");

        var command = Assert.Single(message.Commands);
        Assert.Equal("OUT", command.Verb);
        Assert.Equal("", command.TrId);
    }

    [Fact]
    public void ToleratesCrlfAfterPayload()
    {
        var message = Parse("PUT 7 5\r\nhello\r\nOUT\r\n");

        Assert.Equal(["PUT", "OUT"], message.Commands.Select(c => c.Verb));
        Assert.Equal("hello", message.Commands[0].PayloadText);
    }

    [Theory]
    [InlineData("PUT 7 50\r\nhello")]
    [InlineData("PUT 7\r\nhello")]
    [InlineData("SDG 7 five\r\nhello")]
    [InlineData("SDG 7 -1\r\nhello")]
    public void RejectsBadPayloadFraming(string body)
    {
        Assert.False(MsnpMessage.TryParse(Encoding.UTF8.GetBytes(body), new MessengerOptions().MaxCommandsPerRequest, out _));
    }

    [Fact]
    public void RejectsMoreCommandsThanTheCap()
    {
        var body = Encoding.UTF8.GetBytes("CHL 1\r\nCHL 2\r\nCHL 3\r\n");

        Assert.True(MsnpMessage.TryParse(body, 3, out _));
        Assert.False(MsnpMessage.TryParse(body, 2, out _));
    }

    [Fact]
    public void PayloadIsFramedWithByteLengthAndNoTrailingCrlf()
    {
        var message = MsnpMessage.Of(
            MsnpCommand.Create("NFY", "PUT").WithPayload(AccentedHello),
            MsnpCommand.Create("OUT", ""));

        var expected = Encoding.UTF8.GetBytes("NFY PUT 6\r\n" + AccentedHello + "OUT\r\n");
        Assert.Equal(expected, message.Serialize());
    }

    [Fact]
    public void ErrorReplyIsCodeAndTrId()
    {
        var message = MsnpMessage.Of(MsnpCommand.Error(MsnpErrorCode.AuthenticationFailed, "5"));

        Assert.Equal("911 5\r\n", SerializeToText(message));
    }

    [Fact]
    public void SerializedPayloadsParseBack()
    {
        byte[] binary = [0x00, 0x0D, 0x0A, 0xFF, 0x0D, 0x0A];
        var original = MsnpMessage.Of(
            MsnpCommand.Create("SDG", "11").WithPayload(binary),
            MsnpCommand.Create("PUT", "12").WithPayload(PresencePayload),
            MsnpCommand.Create("CHL", "13"));

        var parsed = Parse(original.Serialize());

        Assert.Equal(["SDG", "PUT", "CHL"], parsed.Commands.Select(c => c.Verb));
        Assert.Equal(binary, parsed.Commands[0].Payload);
        Assert.Equal(PresencePayload, parsed.Commands[1].PayloadText);
        Assert.Null(parsed.Commands[2].Payload);
    }
}
