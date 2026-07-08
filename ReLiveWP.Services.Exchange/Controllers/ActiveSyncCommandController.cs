using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

// Base for all per-command EAS controllers. Provides access to the parsed
// ActiveSyncContext and WBXML serialization helpers.
// Subclasses must also be decorated with [EasCommand(EasCommand.XXX)].
[Authorize]
[ApiController]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public abstract class ActiveSyncCommandController : ControllerBase
{
    protected ActiveSyncContext EasContext =>
        (ActiveSyncContext)HttpContext.Items[ActiveSyncMiddleware.ContextKey]!;

    protected T? DeserializeRequest<T>(XmlDocument doc) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        return (T?)serializer.Deserialize(new XmlNodeReader(doc));
    }

    protected async Task WriteWbxmlResponseAsync<T>(T response, ILogger? logger = null)
        where T : class
    {
        var serializer = new XmlSerializer(typeof(T));

        using var xmlWriter = new StringWriter();
        serializer.Serialize(xmlWriter, response);
        var xml = xmlWriter.ToString();

        var encoder = new ASWBXML();
        encoder.LoadXml(xml);
        var bytes = encoder.GetBytes();

#if DEBUG
        var verify = new ASWBXML();
        verify.LoadBytes(bytes);
        logger?.LogDebug("Sending WBXML response: {Xml}", verify.GetXmlDocument().OuterXml);
        EasContext.OutputXml = verify.GetXmlDocument().OuterXml;
#endif

        HttpContext.Response.ContentType = "application/vnd.ms-sync.wbxml";
        await HttpContext.Response.Body.WriteAsync(bytes);
    }
}
