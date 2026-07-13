using System.Globalization;
using System.Xml;

namespace ReLiveWP.Services.Login;

public static class PassportSoap
{
    public static string FormatZ(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture); 

    public static string BinaryDaTokenEnvelope(string cipherValue, string id = "BinaryDAToken0")
        => $"<EncryptedData xmlns=\"http://www.w3.org/2001/04/xmlenc#\" Id=\"{id}\" Type=\"http://www.w3.org/2001/04/xmlenc#Element\">" +
           "<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#tripledes-cbc\"></EncryptionMethod>" +
           "<ds:KeyInfo xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:KeyName>http://Passport.NET/STS</ds:KeyName></ds:KeyInfo>" +
           $"<CipherData><CipherValue>{cipherValue}</CipherValue></CipherData>" +
           "</EncryptedData>";

    private const string XmlEncNamespace = "http://www.w3.org/2001/04/xmlenc#";

    public static string? CipherValueFromDaTokenWireForm(string? binarySecurityToken)
    {
        if (string.IsNullOrWhiteSpace(binarySecurityToken))
            return null;

        string? da = null;
        foreach (var pair in binarySecurityToken.Trim().Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0 && pair.AsSpan(0, eq).Trim().SequenceEqual("da"))
            {
                da = pair[(eq + 1)..];
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(da))
            return null;

        var encryptedDataXml = Uri.UnescapeDataString(da);

        var document = new XmlDocument();
        try
        {
            document.LoadXml(encryptedDataXml);
        }
        catch (XmlException)
        {
            return null;
        }

        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("xenc", XmlEncNamespace);

        var cipherValue = document.SelectSingleNode("//xenc:CipherData/xenc:CipherValue", ns)?.InnerText;
        return string.IsNullOrWhiteSpace(cipherValue) ? null : cipherValue.Trim();
    }
}
