using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.AppServer.Models;

namespace ReLiveWP.Services.AppServer.Controllers;

[Area("Config")]
public class ConfigController : ControllerBase
{
    private static readonly XmlSerializer ResultsSerializer = new(typeof(RichClientConfigResults));

    [ActionName("GetRichClientConfigRestXml")]
    public IActionResult GetRichClientConfigRestXml(
        [FromQuery] string osName = "windows phone",
        [FromQuery] bool firstRun = false,
        [FromQuery] string osVersion = "7.10",
        [FromQuery] string culture = "en-US",
        [FromQuery] string deviceName = "WP7Device",
        [FromQuery(Name = "AppId")] string? appId = null)
    {
        var results = new RichClientConfigResults
        {
            UserId = new UserIdResult { Value = Guid.NewGuid().ToString("D") }
        };

        return Xml(results, ResultsSerializer);
    }

    private IActionResult Xml(object response, XmlSerializer serializer)
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, string.Empty);

        var stream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
            serializer.Serialize(xmlWriter, response, ns);

        stream.Seek(0, SeekOrigin.Begin);

        return File(stream, "text/xml");
    }
}
