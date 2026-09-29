using System.Text;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpLayeredBodyWriterTests
{
    [Fact]
    public void BlocksEndWithABlankLineAndTheContentFollows()
    {
        var payload = new MsnpLayeredBodyWriter()
            .AddHeader("Routing", "1.0")
            .EndBlock()
            .AddHeader("Content-Length", "2")
            .EndBlock()
            .ToPayload("hi"u8);

        Assert.Equal("Routing: 1.0\r\n\r\nContent-Length: 2\r\n\r\nhi", Encoding.UTF8.GetString(payload));
    }

    [Theory]
    [InlineData("1:bob@example.com\r\nFrom: 1:mallory@example.com")]
    [InlineData("line\nbreak")]
    [InlineData("carriage\rreturn")]
    public void RefusesAValueThatSpansLines(string value)
    {
        Assert.Throws<ArgumentException>(() => new MsnpLayeredBodyWriter().AddHeader("To", value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("To: x")]
    [InlineData("To\r\n")]
    public void RefusesAnUnusableName(string name)
    {
        Assert.Throws<ArgumentException>(() => new MsnpLayeredBodyWriter().AddHeader(name, "1.0"));
    }
}
