using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Docs.Dav;
using ReLiveWP.Services.Grpc.SkyDocs;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Docs.Controllers;

[Authorize]
[Route(DavController.Base)]
public class DavController(SkyDocs.SkyDocsClient client,
                           IHttpClientFactory httpClientFactory,
                           ILogger<DavController> logger) : ControllerBase
{
    public const string Base = "dav";

    private const int MultiStatus = 207;

    [AcceptVerbs("PROPFIND", Route = "{**path}")]
    public async Task<IActionResult> PropFind(string? path)
    {
        var target = (path ?? "").Trim('/');
        var ct = HttpContext.RequestAborted;

        var self = await client.GetItemAsync(new GetDocItemRequest { Path = target }, cancellationToken: ct);
        if (!self.Exists)
            return NotFound();

        var baseUri = $"{Request.Scheme}://{Request.Host}";
        var depth = ParseDepth(Request.Headers["Depth"]);
        var entries = new List<MultiStatusEntry> { ToEntry(baseUri, self.Item, target) };

        if (depth != 0 && self.Item.IsFolder)
        {
            var children = await client.ListItemsAsync(
                new ListDocItemsRequest { Path = target, Recursive = depth == -1 }, cancellationToken: ct);

            entries.AddRange(children.Items.Select(item => ToEntry(baseUri, item, item.Path)));
        }

        logger.LogInformation("PROPFIND {Path} depth {Depth} returning {Count} responses", target, depth, entries.Count);

        return new MultiStatusResult(entries);
    }

    [HttpGet("{**path}")]
    [HttpHead("{**path}")]
    public async Task<IActionResult> Get(string? path)
    {
        var target = (path ?? "").Trim('/');
        var ct = HttpContext.RequestAborted;

        var location = await client.GetContentLocationAsync(
            new GetDocContentLocationRequest { Path = target }, cancellationToken: ct);

        if (!location.Exists)
            return NotFound();

        var content = new ContentLocation(location.Url, location.Headers, location.ContentType, location.Etag);

        using var http = httpClientFactory.CreateClient();
        using var response = await http.FetchAsync(content, HttpContext, ct);

        await response.PipeAsync(content, HttpContext, ct);
        return new EmptyResult();
    }

    private static int ParseDepth(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "0" => 0,
        "infinity" => -1,
        _ => 1,
    };

    private static MultiStatusEntry ToEntry(string baseUri, DocItem item, string path) => new(
        DavUrls.Build(baseUri, path),
        string.IsNullOrEmpty(item.Name) ? DavUrls.Name(path) : item.Name,
        item.IsFolder,
        item.Size,
        DateTimeOffset.FromUnixTimeSeconds(item.CreatedUnix),
        DateTimeOffset.FromUnixTimeSeconds(item.ModifiedUnix),
        item.ReadOnly,
        item.ResourceId,
        string.IsNullOrEmpty(item.ProgId) ? null : item.ProgId);

    private sealed class MultiStatusResult(IReadOnlyList<MultiStatusEntry> entries) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = MultiStatus;
            response.ContentType = "text/xml; charset=utf-8";

            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
            };

            await using var writer = XmlWriter.Create(response.Body, settings);
            MultiStatusWriter.Write(writer, entries);
            await writer.FlushAsync();
        }
    }
}
