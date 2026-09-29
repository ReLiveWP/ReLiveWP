using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpCommandLogTests
{
    [Fact]
    public void SsoTicketAndSecretAreRedacted()
    {
        var usr = Assert.Single(Parse($"USR 6 SSO S t=eyJhbGciOi.payload.sig&p= HAAAAAEAAAAD {Epid}\r\n").Commands);

        var line = usr.ToLogString();

        Assert.Equal($"USR 6 SSO S <redacted> <redacted> {Epid}", line);
    }

    [Fact]
    public void SignInUsrIsLoggedAsIs()
    {
        var usr = Assert.Single(Parse("USR 5 SSO I alice@example.com\r\n").Commands);

        Assert.Equal("USR 5 SSO I alice@example.com", usr.ToLogString());
    }

    [Fact]
    public void MessageTextIsLeftOut()
    {
        const string payload =
            "Routing: 1.0\r\n" +
            "To: 1:bob@example.com\r\n" +
            "From: 1:alice@example.com;epid=" + Epid + "\r\n" +
            "Service-Channel: IM/Online\r\n" +
            "\r\n" +
            "Reliability: 1.0\r\n" +
            "\r\n" +
            "Messaging: 2.0\r\n" +
            "Content-Type: text/plain; charset=UTF-8\r\n" +
            "Content-Length: 12\r\n" +
            "Message-Type: Text\r\n" +
            "Content-Transfer-Encoding: 7bit\r\n" +
            "\r\n" +
            "secret words";
        var sdg = Assert.Single(Parse($"SDG 11 {payload.Length}\r\n{payload}").Commands);

        var line = sdg.ToLogString();

        Assert.DoesNotContain("secret words", line);
        Assert.StartsWith($"SDG 11 <{payload.Length} bytes>", line);
        Assert.Contains("To=1:bob@example.com", line);
        Assert.Contains("Service-Channel=IM/Online", line);
        Assert.Contains("Message-Type=Text", line);
    }
}
