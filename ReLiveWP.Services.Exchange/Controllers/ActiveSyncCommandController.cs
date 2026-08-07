using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

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
        EasContext.OutputXml = verify.GetXmlDocument().OuterXml;
#else
        EasContext.OutputXml = xml;
#endif

        HttpContext.Response.ContentType = "application/vnd.ms-sync.wbxml";
        HttpContext.Response.ContentLength = bytes.Length;
        await HttpContext.Response.Body.WriteAsync(bytes);
    }

    protected async Task WriteMultiPartResponseAsync<T>(T response, IReadOnlyList<byte[]> parts, ILogger? logger = null)
        where T : class
    {
        var serializer = new XmlSerializer(typeof(T));

        using var xmlWriter = new StringWriter();
        serializer.Serialize(xmlWriter, response);
        var xml = xmlWriter.ToString();

        var encoder = new ASWBXML();
        encoder.LoadXml(xml);
        var wbxml = encoder.GetBytes();

#if DEBUG
        var verify = new ASWBXML();
        verify.LoadBytes(wbxml);
        EasContext.OutputXml = verify.GetXmlDocument().OuterXml;
#else
        EasContext.OutputXml = xml;
#endif

        var partsCount = 1 + parts.Count;
        var headerSize = 4 + partsCount * 8;

        var lengths = new int[partsCount];
        lengths[0] = wbxml.Length;
        for (int i = 0; i < parts.Count; i++)
            lengths[i + 1] = parts[i].Length;

        var offsets = new int[partsCount];
        offsets[0] = headerSize;
        for (int i = 1; i < partsCount; i++)
            offsets[i] = offsets[i - 1] + lengths[i - 1];

        var body = new byte[offsets[^1] + lengths[^1]];
        var span = body.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span, partsCount);
        for (int i = 0; i < partsCount; i++)
        {
            var meta = span.Slice(4 + i * 8, 8);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(meta, offsets[i]);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(meta.Slice(4), lengths[i]);
        }

        wbxml.CopyTo(span.Slice(offsets[0]));
        for (int i = 0; i < parts.Count; i++)
            parts[i].CopyTo(span.Slice(offsets[i + 1]));

        HttpContext.Response.ContentType = "application/vnd.ms-sync.multipart";
        HttpContext.Response.ContentLength = body.Length;
        await HttpContext.Response.Body.WriteAsync(body);
    }
}
