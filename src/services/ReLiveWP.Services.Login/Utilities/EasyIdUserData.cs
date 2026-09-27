using System.Xml;

namespace ReLiveWP.Services.Login.Utilities;

public record EasyIdUserData(
    string MemberName,
    string? AlternateEmail,
    string Ski,
    string CipherValue,
    string? Version);

public static class EasyIdUserDataParser
{
    private const string PassportUserNamespace = "http://schemas.microsoft.com/Passport/User";

    public static EasyIdUserData? ParseUserData(string xml)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml(xml);
        }
        catch (XmlException)
        {
            return null;
        }

        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("p", PassportUserNamespace);

        var credential = document.SelectSingleNode("/p:userData/p:credential", ns);
        if (credential == null)
            return null;

        var memberName = credential.SelectSingleNode("p:property[@name='Name']", ns)?.InnerText;
        var alternateEmail = credential.SelectSingleNode("p:property[@name='AlternateEmail']", ns)?.InnerText;
        var ski = credential.SelectSingleNode("p:EncryptedProperties/p:x509SKI", ns)?.InnerText;
        var cipherValue = credential.SelectSingleNode("p:EncryptedProperties/p:CipherValue", ns)?.InnerText;
        var version = credential.SelectSingleNode("p:EncryptedProperties/p:Version", ns)?.InnerText;

        if (string.IsNullOrWhiteSpace(memberName) || string.IsNullOrWhiteSpace(ski) || string.IsNullOrWhiteSpace(cipherValue))
            return null;

        return new EasyIdUserData(memberName, alternateEmail, ski, cipherValue, version);
    }
}
