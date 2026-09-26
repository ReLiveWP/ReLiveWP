using System.Net.Http.Headers;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class MastodonServiceProxy(IServiceProvider services)
    : ConnectedServiceProxyBase<MastodonOAuthProvider>(Mastodon.SERVICE_NAME, services)
{
    public override Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(HttpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName));

    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);
        return Task.CompletedTask;
    }
}
