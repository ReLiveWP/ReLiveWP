using System.Text;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Tests;

internal static class MsnpSamples
{
    public const string Epid = "{11111111-2222-3333-4444-555555555555}";

    public const string PresencePayload =
        "Routing: 1.0\r\n" +
        "To: 1:alice@example.com\r\n" +
        "From: 1:alice@example.com;epid=" + Epid + "\r\n" +
        "\r\n" +
        "Reliability: 1.0\r\n" +
        "\r\n" +
        "Publication: 1.0\r\n" +
        "Uri: /user\r\n" +
        "Content-Type: application/user+xml\r\n" +
        "Content-Length: 47\r\n" +
        "\r\n" +
        "<user><s n=\"IM\"><Status>NLN</Status></s></user>";

    public static string SelfPresencePut(string trId) =>
        $"PUT {trId} {Encoding.UTF8.GetByteCount(PresencePayload)}\r\n{PresencePayload}";

    public static string SdgPayload(
        string body,
        string to = "1:bob@example.com",
        string from = "1:alice@example.com;epid=" + Epid,
        string channel = "IM/Online",
        string messageType = "Text") =>
        "Routing: 1.0\r\n" +
        $"To: {to}\r\n" +
        $"From: {from}\r\n" +
        $"Service-Channel: {channel}\r\n" +
        "\r\n" +
        "Reliability: 1.0\r\n" +
        "\r\n" +
        "Messaging: 2.0\r\n" +
        "Content-Type: text/plain; charset=UTF-8\r\n" +
        $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
        $"Message-Type: {messageType}\r\n" +
        "Content-Transfer-Encoding: 7bit\r\n" +
        "\r\n" +
        body;

    public static string Sdg(string trId, string payload) =>
        $"SDG {trId} {Encoding.UTF8.GetByteCount(payload)}\r\n{payload}";

    public static MsnpMessage Parse(byte[] body)
    {
        Assert.True(MsnpMessage.TryParse(body, new MessengerOptions().MaxCommandsPerRequest, out var message));
        return message;
    }

    public static MsnpMessage Parse(string body) => Parse(Encoding.UTF8.GetBytes(body));

    public static string SerializeToText(MsnpMessage message) => Encoding.UTF8.GetString(message.Serialize());
}
