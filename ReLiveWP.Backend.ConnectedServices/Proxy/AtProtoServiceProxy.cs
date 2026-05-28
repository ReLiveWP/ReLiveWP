using System.Net;
using System.Net.Http.Headers;
using Duende.IdentityModel.OidcClient.DPoP;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class AtProtoServiceProxy(AtProtoOAuthProvider oauthProvider,
                                  IJWKProvider jwkProvider,
                                  IHttpMessageHandlerFactory handlerFactory,
                                  ILogger<AtProtoServiceProxy> logger) : IConnectedServiceProxy
{
    public string ServiceId => AtProto.SERVICE_NAME;

    public Task<bool> RefreshAsync(LiveConnectedService service, CancellationToken ct = default) =>
        oauthProvider.RefreshTokensAsync(service);

    public async Task SendProxiedRequestAsync(LiveConnectedService service, HttpContext context, string path, CancellationToken ct = default)
    {
        var key = await jwkProvider.GetJWKAsync(service.DPoPKeyId!);

        using var innerHandler = handlerFactory.CreateHandler();
        using var tokenHandler = new ProofTokenMessageHandler(key, innerHandler);
        using var client = new HttpClient(tokenHandler);

        var targetUrl = new Uri(new Uri(service.ServiceUrl!), "/" + path + context.Request.QueryString);
        using var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("DPoP", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("X-Connection-ID", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;

            targetRequest.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        targetRequest.Headers.Authorization = new AuthenticationHeaderValue("DPoP", service.AccessToken);

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
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await resp.Content.CopyToAsync(context.Response.Body, ct);
    }
}
