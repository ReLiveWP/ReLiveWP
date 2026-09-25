using System.Net;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class ConnectedServiceProxyBase(string serviceId, IServiceProvider services)
    : IConnectedServiceProxy
{
    private static readonly HashSet<string> ExcludedResponseHeaders
        = new(StringComparer.OrdinalIgnoreCase) { "Transfer-Encoding", "Content-Length" };

    private static readonly HashSet<string> ExcludedRequestHeaders
        = new(ExcludedResponseHeaders, StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Host", "DPoP", "X-User-ID", "X-Connection-ID"
        };

    private readonly ILogger<ConnectedServiceProxyBase> logger
        = services.GetRequiredService<ILoggerFactory>().CreateLogger<ConnectedServiceProxyBase>();

    protected IServiceProvider Services { get; } = services;

    protected IHttpClientFactory HttpClientFactory { get; }
        = services.GetRequiredService<IHttpClientFactory>();

    public string ServiceId { get; } = serviceId;

    public virtual Task<bool> RefreshAsync(LiveConnectedService service, CancellationToken ct = default)
        => Task.FromResult(true);

    public async Task SendProxiedRequestAsync(LiveConnectedService service, HttpContext context, string path, CancellationToken ct = default)
    {
        using var client = await this.CreateHttpClientAsync(service);

        var targetUrl = this.GetRequestUrl(service, context, path);
        using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        foreach (var header in context.Request.Headers)
        {
            if (ExcludedRequestHeaders.Contains(header.Key) || FilterRequestHeaders(service, header.Key))
                continue;

            targetRequest.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        await this.AddHeadersAsync(service, targetRequest);

        if (context.Request.ContentLength > 0 || context.Request.ContentType is not null)
        {
            targetRequest.Content = new StreamContent(context.Request.Body);

            if (PreserveContentLength && context.Request.ContentLength is { } contentLength)
                targetRequest.Content.Headers.ContentLength = contentLength;
            else
                targetRequest.Headers.TransferEncodingChunked = true;

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
            if (ExcludedResponseHeaders.Contains(header.Key) || FilterResponseHeaders(service, header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await resp.Content.CopyToAsync(context.Response.Body, ct);
    }

    public virtual bool PreserveContentLength => false;

    public virtual Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
        => Task.CompletedTask;
    public virtual Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(HttpClientFactory.CreateClient());
    public virtual Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
    {
        var serviceUrl = new Uri(service.ServiceUrl!);
        var target = new Uri(serviceUrl, "/" + path + context.Request.QueryString);

        // a path starting "//" or "/\" is protocol-relative and would swap the host out
        if (Uri.Compare(target, serviceUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
            throw new InvalidOperationException($"{target.Authority} is not part of the linked {ServiceId} account.");

        return target;
    }
    public virtual bool FilterRequestHeaders(LiveConnectedService service, string header)
        => false;
    public virtual bool FilterResponseHeaders(LiveConnectedService service, string header)
        => false;
}

public class ConnectedServiceProxyBase<T>(string serviceId, IServiceProvider services)
    : ConnectedServiceProxyBase(serviceId, services) where T : IOAuthProvider
{
    public override async Task<bool> RefreshAsync(LiveConnectedService service, CancellationToken ct = default)
        => await Services.GetRequiredService<T>().RefreshTokensAsync(service);
}
