using System.Xml;

namespace ReLiveWP.Services.Login.Utilities;

public static class SignupServiceSoap
{
    private const string SignupNamespace = "http://schemas.microsoft.com/Passport/Mobile/WebServices/Signup/V1";

    public static string? ReadSigninName(string body)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml(body);
        }
        catch (XmlException)
        {
            return null;
        }

        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("s", SignupNamespace);

        return document.SelectSingleNode("//s:CheckSigninNameAvailability/s:SigninName", ns)?.InnerText.Trim();
    }
}
