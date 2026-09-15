using System.Net;
using System.Text;
using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// A stand-in for the upstream service that behaves like the real one in the way that matters here:
// it rejects any report larger than its cap, so the crawler has to split its way down to a size the
// server will answer.
public class FakeUpdateServerHandler(int detectoidCount, int reportCap) : HttpMessageHandler
{
    private const int PageSize = 90;
    private const int RequestLimit = 2000;

    public int RequestCount { get; private set; }
    public int FaultCount { get; private set; }

    private static XNamespace Ns => WsupXml.ServiceNs;
    private static XNamespace Soap => WsupXml.SoapNs;

    private long DetectoidRevision(int index) => 1000 + index;
    private long LeafRevision(int index) => 2000 + index;
    private static Guid FakeGuid(long revision) => new((int)revision, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (++RequestCount > RequestLimit)
            throw new InvalidOperationException($"crawler issued more than {RequestLimit} requests, it is not converging");

        var action = request.Headers.GetValues("SOAPAction").Single().Trim('"');
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);

        var responseXml = action[(action.LastIndexOf('/') + 1)..] switch
        {
            "GetConfig" => Envelope(new XElement(Ns + "GetConfigResponse",
                new XElement(Ns + "GetConfigResult", new XElement(Ns + "LastChange", "2017-03-08T21:07:43.58Z")))),
            "GetCookie" => Envelope(new XElement(Ns + "GetCookieResponse",
                new XElement(Ns + "GetCookieResult", Cookie()))),
            "SyncUpdates" => SyncUpdates(body),
            "GetExtendedUpdateInfo" => Envelope(new XElement(Ns + "GetExtendedUpdateInfoResponse",
                new XElement(Ns + "GetExtendedUpdateInfoResult",
                    new XElement(Ns + "Updates"), new XElement(Ns + "FileLocations")))),
            _ => Fault("InvalidParameters"),
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseXml, Encoding.UTF8, "text/xml"),
        };
    }

    private string SyncUpdates(string body)
    {
        var installed = ReadIds(body, "InstalledNonLeafUpdateIDs");
        var cached = ReadIds(body, "OtherCachedUpdateIDs");

        if (installed.Count > reportCap)
        {
            FaultCount++;
            return Fault("InvalidParameters");
        }

        var offered = new List<XElement>();

        for (var i = 0; i < detectoidCount; i++)
            if (!cached.Contains(DetectoidRevision(i)) && !installed.Contains(DetectoidRevision(i)))
                offered.Add(UpdateInfo(DetectoidRevision(i), isLeaf: false, gatedBy: null));

        for (var i = 0; i < detectoidCount; i++)
            if (installed.Contains(DetectoidRevision(i)) && !cached.Contains(LeafRevision(i)))
                offered.Add(UpdateInfo(LeafRevision(i), isLeaf: true, gatedBy: DetectoidRevision(i)));

        var page = offered.Take(PageSize).ToList();

        return Envelope(new XElement(Ns + "SyncUpdatesResponse",
            new XElement(Ns + "SyncUpdatesResult",
                new XElement(Ns + "NewUpdates", page),
                new XElement(Ns + "ChangedUpdates"),
                new XElement(Ns + "Truncated", offered.Count > page.Count),
                new XElement(Ns + "NewCookie", Cookie()))));
    }

    private XElement UpdateInfo(long revision, bool isLeaf, long? gatedBy)
    {
        var identity = new XElement("UpdateIdentity",
            new XAttribute("UpdateID", FakeGuid(revision)),
            new XAttribute("RevisionNumber", 1));

        var properties = new XElement("Properties",
            new XAttribute("UpdateType", isLeaf ? "Software" : "Detectoid"));

        var fragment = new StringBuilder();
        fragment.Append(identity).Append(properties);

        if (gatedBy is not null)
        {
            fragment.Append(new XElement("Relationships",
                new XElement("Prerequisites",
                    new XElement("UpdateIdentity", new XAttribute("UpdateID", FakeGuid(gatedBy.Value))))));
        }

        return new XElement(Ns + "UpdateInfo",
            new XElement(Ns + "ID", revision),
            new XElement(Ns + "Deployment",
                new XElement(Ns + "ID", revision + 100000),
                new XElement(Ns + "Action", isLeaf ? "Install" : "Evaluate"),
                new XElement(Ns + "IsAssigned", "true"),
                new XElement(Ns + "LastChangeTime", "2011-03-02")),
            new XElement(Ns + "IsLeaf", isLeaf),
            new XElement(Ns + "Xml", fragment.ToString()));
    }

    private static object[] Cookie() =>
    [
        new XElement(Ns + "Expiration", DateTime.UtcNow.AddDays(1).ToString("s")),
        new XElement(Ns + "EncryptedData", Convert.ToBase64String(Guid.NewGuid().ToByteArray())),
    ];

    private static HashSet<long> ReadIds(string body, string element)
    {
        var container = XDocument.Parse(body).Descendants().FirstOrDefault(e => e.Name.LocalName == element);
        if (container is null)
            return [];

        return container.Elements().Where(e => e.Name.LocalName == "int").Select(e => (long)e).ToHashSet();
    }

    private static string Fault(string errorCode) =>
        new XDocument(new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "s", WsupXml.SoapNs),
            new XElement(Soap + "Body",
                new XElement(Soap + "Fault",
                    new XElement("faultcode", "s:Client"),
                    new XElement("faultstring", "Fault occurred"),
                    new XElement("detail",
                        new XElement("ErrorCode", errorCode),
                        new XElement("Message"),
                        new XElement("ID", Guid.NewGuid())))))).ToString(SaveOptions.DisableFormatting);

    private static string Envelope(XElement payload) =>
        new XDocument(new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "s", WsupXml.SoapNs),
            new XElement(Soap + "Body", payload))).ToString(SaveOptions.DisableFormatting);
}
