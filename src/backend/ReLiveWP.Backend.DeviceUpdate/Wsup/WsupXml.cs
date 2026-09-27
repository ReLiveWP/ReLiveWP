using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// Shared constants and helpers for the WSUS client web service wire format.
public static class WsupXml
{
    public const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    public const string ServiceNs = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";
    public static readonly Guid WindowsPhoneCategoryId = new("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8");

    public static readonly XNamespace Soap = SoapNs;
    public static readonly XNamespace Service = ServiceNs;

    public static string BuildResponse(string responseElement, params object[] content)
    {
        var envelope = new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "s", SoapNs),
            new XElement(Soap + "Body",
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XElement(Service + responseElement,
                    new XAttribute("xmlns", ServiceNs),
                    content)));

        return new XDocument(envelope).ToString(SaveOptions.DisableFormatting);
    }

    public static string? ReadFaultCode(string responseXml)
    {
        if (!responseXml.Contains("<ErrorCode>", StringComparison.Ordinal))
            return null;

        return XDocument.Parse(responseXml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ErrorCode")?.Value.Trim();
    }

    public static bool IsCookieFault(string errorCode) =>
        errorCode is "CookieExpired" or "InvalidCookie" or "ServerChanged" or "ConfigChanged";

    public static DateTime? AsUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

    public static XElement ParseFragment(string innerXml)
        => XElement.Parse("<root>" + innerXml + "</root>", LoadOptions.PreserveWhitespace);

    public static List<(string Digest, string Url)> ParseFileLocations(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var result = new List<(string, string)>();
        foreach (var loc in doc.Descendants().Where(e => e.Name.LocalName == "FileLocation"))
        {
            var digest = loc.Elements().FirstOrDefault(e => e.Name.LocalName == "FileDigest")?.Value;
            var url = loc.Elements().FirstOrDefault(e => e.Name.LocalName == "Url")?.Value;
            if (!string.IsNullOrEmpty(url))
                result.Add((digest ?? "", url));
        }
        return result;
    }
}
