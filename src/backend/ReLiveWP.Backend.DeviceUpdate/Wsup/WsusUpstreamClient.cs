using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public class WsusUpstreamClient(HttpClient http, IOptions<CrawlerOptions> options)
{
    private const string ActionBase = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/";
    private const string ProtocolVersion = "1.8";

    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace SoapEnc = "http://schemas.xmlsoap.org/soap/encoding/";
    private static readonly XNamespace Service = WsupXml.ServiceNs;

    private static readonly string[] Locales =
    [
        "zh-TW", "zh-Ha", "cs-CZ", "cs", "da-DK", "da", "de-DE", "de", "el-GR", "el", "en-US", "en",
        "fi-FI", "fi", "fr-FR", "fr", "hu-HU", "hu", "it-IT", "it", "ja-JP", "ja", "ko-KR", "ko",
        "nl-NL", "nl", "nb-NO", "no", "pl-PL", "pl", "pt-BR", "pt", "ru-RU", "ru", "sv-SE", "sv",
        "zh-CN", "zh-Ha", "en-GB", "en", "pt-PT", "pt", "es-ES", "es",
    ];

    private readonly string clientEndpoint = options.Value.ClientEndpoint;

    public async Task<WsusCookie> GetFreshCookieAsync()
    {
        var configResponse = await PostAsync("GetConfig",
            new XElement(Service + "protocolVersion", ProtocolVersion));

        var lastChange = XDocument.Parse(configResponse)
            .Descendants().First(e => e.Name.LocalName == "LastChange").Value;

        var cookieData = Convert.ToBase64String([.. Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()), 0]);

        var cookieResponse = await PostAsync("GetCookie",
            new XElement(Service + "authCookies",
                new XAttribute(XNamespace.Xmlns + "q1", WsupXml.ServiceNs),
                new XAttribute(SoapEnc + "arrayType", "q1:AuthorizationCookie[2]"),
                AuthorizationCookie("PidValidator", cookieData),
                AuthorizationCookie("Anonymous", cookieData)),
            new XElement(Service + "oldCookie",
                new XElement(Service + "Expiration", lastChange),
                new XElement(Service + "EncryptedData", new XAttribute(Xsi + "nil", "1"))),
            new XElement(Service + "lastChange", lastChange),
            new XElement(Service + "currentTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XElement(Service + "protocolVersion", ProtocolVersion));

        return WsusCookie.Parse(cookieResponse)
            ?? throw new InvalidOperationException("GetCookie did not return a cookie: " + cookieResponse);
    }

    public Task<string> SyncUpdatesAsync(WsusCookie cookie, IReadOnlyCollection<long> installedNonLeaf, IReadOnlyCollection<long> otherCached) =>
        PostAsync("SyncUpdates",
            CookieElement(cookie),
            new XElement(Service + "parameters",
                new XElement(Service + "ExpressQuery", "false"),
                IntArray("InstalledNonLeafUpdateIDs", installedNonLeaf),
                IntArray("OtherCachedUpdateIDs", otherCached),
                new XElement(Service + "SkipSoftwareSync", "false"),
                new XElement(Service + "FilterCategoryIds",
                    new XAttribute(XNamespace.Xmlns + "q1", WsupXml.ServiceNs),
                    new XAttribute(SoapEnc + "arrayType", "q1:CategoryIdentifier[1]"),
                    new XElement(Service + "CategoryIdentifier",
                        new XElement(Service + "Id", WsupXml.WindowsPhoneCategoryId))),
                new XElement(Service + "NeedTwoGroupOutOfScopeUpdates", "false")));

    public Task<string> GetExtendedUpdateInfoAsync(WsusCookie cookie, IReadOnlyCollection<long> revisionIds) =>
        PostAsync("GetExtendedUpdateInfo",
            CookieElement(cookie),
            IntArray("revisionIDs", revisionIds),
            new XElement(Service + "infoTypes",
                new XAttribute(XNamespace.Xmlns + "q1", WsupXml.ServiceNs),
                new XAttribute(SoapEnc + "arrayType", "q1:XmlUpdateFragmentType[3]"),
                FragmentType("Extended"),
                FragmentType("LocalizedProperties"),
                FragmentType("Eula")),
            new XElement(Service + "locales",
                new XAttribute(SoapEnc + "arrayType", $"xsd:string[{Locales.Length}]"),
                Locales.Select(l => new XElement(Service + "string", l))));

    public Task<string> GetFileLocationsAsync(WsusCookie cookie, IReadOnlyCollection<string> base64Sha1Digests) =>
        PostAsync("GetFileLocations",
            CookieElement(cookie),
            new XElement(Service + "fileDigests",
                new XAttribute(SoapEnc + "arrayType", $"xsd:base64Binary[{base64Sha1Digests.Count}]"),
                base64Sha1Digests.Select(d => new XElement(Service + "base64Binary", d))));

    private static XElement AuthorizationCookie(string plugInId, string cookieData) =>
        new(Service + "AuthorizationCookie",
            new XElement(Service + "PlugInId", plugInId),
            new XElement(Service + "CookieData", cookieData));

    private static XElement FragmentType(string name) =>
        new(Service + "XmlUpdateFragmentType", name);

    private static XElement CookieElement(WsusCookie cookie) =>
        new(Service + "cookie",
            new XElement(Service + "Expiration", cookie.Expiration),
            new XElement(Service + "EncryptedData", cookie.EncryptedData));

    private static XElement IntArray(string name, IReadOnlyCollection<long> values)
    {
        if (values.Count == 0)
            return new XElement(Service + name, new XAttribute(Xsi + "nil", "1"));

        return new XElement(Service + name,
            new XAttribute(SoapEnc + "arrayType", $"xsd:int[{values.Count}]"),
            values.Select(v => new XElement(Service + "int", v)));
    }

    private async Task<string> PostAsync(string operation, params object[] content)
    {
        var envelope = new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "soapenc", SoapEnc.NamespaceName),
            new XElement(Soap + "Body",
                new XElement(Service + operation,
                    new XAttribute("xmlns", WsupXml.ServiceNs),
                    content)));

        var body = new XDocument(envelope).ToString(SaveOptions.DisableFormatting);

        var request = new HttpRequestMessage(HttpMethod.Post, clientEndpoint);
        request.Headers.Add("SOAPAction", $"\"{ActionBase}{operation}\"");
        request.Content = new StringContent(body, new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" });

        // faults arrive as HTTP 500 with a SOAP fault body, so callers read <ErrorCode> themselves
        var response = await http.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
