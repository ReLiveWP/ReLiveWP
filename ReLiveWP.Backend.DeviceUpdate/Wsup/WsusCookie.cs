using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public record WsusCookie(string Expiration, string EncryptedData)
{
    public static WsusCookie IssueCookie() => new(
        DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss.ffffZ"),
        Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

    public XElement ToResponse(XNamespace ns, string resultElement) =>
        new(ns + resultElement,
            new XElement(ns + "Expiration", Expiration),
            new XElement(ns + "EncryptedData", EncryptedData));

    public static WsusCookie? Parse(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);

        var expiration = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Expiration");
        var data = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "EncryptedData");
        if (expiration is null || data is null)
            return null;

        return new WsusCookie(expiration.Value.Trim(), data.Value.Trim());
    }
}
