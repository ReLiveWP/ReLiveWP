using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public static class MsnpNotifications
{
    public static MsnpCommand PresenceNfy(
        string toEmail, string? toEpid, string fromMuri, string presenceXml,
        string notifType = "Full", int notifNum = 0)
    {
        var contentLength = Encoding.UTF8.GetByteCount(presenceXml);
        var toHeader = string.IsNullOrEmpty(toEpid) ? $"1:{toEmail}" : $"1:{toEmail};epid={{{toEpid}}}";

        var body = new StringBuilder()
            .Append("Routing: 1.0\r\n")
            .Append($"To: {toHeader}\r\n")
            .Append($"From: {fromMuri}\r\n")
            .Append("\r\n")
            .Append("Reliability: 1.0\r\n")
            .Append("\r\n")
            .Append("Notification: 1.0\r\n")
            .Append($"NotifNum: {notifNum}\r\n")
            .Append("Uri: /user\r\n")
            .Append($"NotifType: {notifType}\r\n")
            .Append("Content-Type: application/user+xml\r\n")
            .Append($"Content-Length: {contentLength}\r\n")
            .Append("\r\n")
            .Append(presenceXml)
            .ToString();

        return MsnpCommand.Create("NFY", "PUT").WithPayload(body);
    }

    public static string PresenceDocument(string status = "NLN", string? friendlyName = null)
    {
        var sb = new StringBuilder("<user><s n=\"IM\"><Status>");
        sb.Append(status).Append("</Status></s>");
        if (!string.IsNullOrEmpty(friendlyName))
            sb.Append("<s n=\"PE\"><FriendlyName>").Append(friendlyName).Append("</FriendlyName></s>");
        sb.Append("</user>");
        return sb.ToString();
    }
}
