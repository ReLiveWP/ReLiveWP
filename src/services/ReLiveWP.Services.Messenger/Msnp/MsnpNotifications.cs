using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Services.Messenger.Msnp;

public static class MsnpNotifications
{
    public static MsnpCommand PresenceNfy(
        MsnpMuri to, MsnpMuri from, XElement user, MsnpNotifType notifType = MsnpNotifType.Full, int notifNum = 0) =>
        UserNotification("PUT", to, from, user, notifType, notifNum);

    public static MsnpCommand PresenceRemovedNfy(MsnpMuri to, MsnpMuri from, int notifNum = 0) =>
        UserNotification("DEL", to, from, new XElement("user", ImService()), MsnpNotifType.Partial, notifNum);

    public static MsnpCommand EndpointRemovedNfy(MsnpMuri to, MsnpMuri from, Guid epid, int notifNum = 0) =>
        UserNotification("DEL", to, from, new XElement("user", ImEndpoint(epid)), MsnpNotifType.Partial, notifNum);

    public static XElement PresenceDocument(PresenceStatus status, IEnumerable<Guid> endpointIds)
    {
        var imStatus = new XElement("Status", MsnpPresenceStatus.Format(status));
        var imEndpoints = endpointIds.Select(ImEndpoint);
        return new XElement("user", ImService(imStatus), imEndpoints);
    }

    private static XElement ImService(params object[] content) =>
        new("s", new XAttribute("n", "IM"), content);

    private static XElement ImEndpoint(Guid epid) =>
        new("sep", new XAttribute("n", "IM"), new XAttribute("epid", MsnpMuri.FormatEpid(epid)));

    private static MsnpCommand UserNotification(
        string action, MsnpMuri to, MsnpMuri from, XElement user, MsnpNotifType notifType, int notifNum)
    {
        var content = Encoding.UTF8.GetBytes(user.ToString(SaveOptions.DisableFormatting));

        var payload = new MsnpLayeredBodyWriter()
            .AddHeader("Routing", "1.0")
            .AddHeader("To", to.ToString())
            .AddHeader("From", from.ToString())
            .EndBlock()
            .AddHeader("Reliability", "1.0")
            .EndBlock()
            .AddHeader("Notification", "1.0")
            .AddHeader("NotifNum", notifNum.ToString(CultureInfo.InvariantCulture))
            .AddHeader("Uri", "/user")
            .AddHeader("NotifType", notifType.ToString())
            .AddHeader("Content-Type", "application/user+xml")
            .AddHeader("Content-Length", content.Length.ToString(CultureInfo.InvariantCulture))
            .EndBlock()
            .ToPayload(content);

        return MsnpCommand.Create("NFY", action).WithPayload(payload);
    }
}
