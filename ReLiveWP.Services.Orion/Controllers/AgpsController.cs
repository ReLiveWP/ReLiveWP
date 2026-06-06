using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Orion.Controllers;

[ApiController]
[Route("[controller]/v21/pox/{action}")]
public class AgpsController(ILogger<AgpsController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    [ActionName("GetAgpsData")]
    public async Task<IActionResult> GetAgpsDataAsync(CancellationToken ct = default)
    {
        // agps.location.live.net still _works_ but Windows Phone 7 defaults to doing it over HTTP, and then fails when the remote server
        // redirects to HTTPS. we're just proxying forward

        var context = this.HttpContext;

        using var client = httpClientFactory.CreateClient();
        var targetUrl = new Uri(configuration["Orion:Upstream:LiveAgpsService"] + context.Request.QueryString);
        using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;

            targetRequest.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        if (context.Request.ContentLength > 0)
        {
            targetRequest.Headers.TransferEncodingChunked = true;
            targetRequest.Content = new StreamContent(context.Request.Body);

            if (context.Request.ContentType is { } contentType)
                targetRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        using var resp = await client.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        context.Response.StatusCode = (int)resp.StatusCode;

        foreach (var header in resp.Headers.Concat(resp.Content.Headers))
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await resp.Content.CopyToAsync(context.Response.Body, ct);

        return Empty;
    }
}
