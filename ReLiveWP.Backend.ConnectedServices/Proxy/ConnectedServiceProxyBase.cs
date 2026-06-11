using System.Net;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class ConnectedServiceProxyBase<T>(string serviceId, IServiceProvider services)
    : IConnectedServiceProxy where T : IOAuthProvider
{
    private readonly ILogger<ConnectedServiceProxyBase<T>> logger
        = services.GetRequiredService<ILoggerFactory>().CreateLogger<ConnectedServiceProxyBase<T>>();
    private readonly IHttpClientFactory httpClientFactory
        = services.GetRequiredService<IHttpClientFactory>();

    public string ServiceId { get; } = serviceId;

    public async Task<bool> RefreshAsync(LiveConnectedService service, CancellationToken ct = default)
        => await services.GetRequiredService<T>().RefreshTokensAsync(service);

    public async Task SendProxiedRequestAsync(LiveConnectedService service, HttpContext context, string path, CancellationToken ct = default)
    {
        using var client = await this.CreateHttpClientAsync(service);

        var targetUrl = this.GetRequestUrl(service, context, path);
        using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("DPoP", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("X-Connection-ID", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                FilterRequestHeaders(service, header.Key))
                continue;

            targetRequest.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        await this.AddHeadersAsync(service, targetRequest);

        if (context.Request.ContentLength > 0)
        {
            targetRequest.Headers.TransferEncodingChunked = true;
            targetRequest.Content = new StreamContent(context.Request.Body);

            if (context.Request.ContentType is { } contentType)
                targetRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        using var resp = await client.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            service.Flags |= LiveConnectedServiceFlags.NeedsRefresh;
            logger.LogWarning("Upstream returned 401 for {ServiceId}, flagging for refresh", service.Id);
        }

        context.Response.StatusCode = (int)resp.StatusCode;

        foreach (var header in resp.Headers.Concat(resp.Content.Headers))
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                FilterResponseHeaders(service, header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await resp.Content.CopyToAsync(context.Response.Body, ct);
    }

    public virtual Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
        => Task.CompletedTask;
    public virtual Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(httpClientFactory.CreateClient());
    public virtual Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
        => new Uri(new Uri(service.ServiceUrl!), "/" + path + context.Request.QueryString);
    public virtual bool FilterRequestHeaders(LiveConnectedService service, string header)
        => false;
    public virtual bool FilterResponseHeaders(LiveConnectedService service, string header)
        => false;
}
