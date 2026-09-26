using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public class MastodonActivityProviderFactory(IConfiguration configuration,
                                             IHttpClientFactory httpClientFactory,
                                             MastodonActorResolver resolver,
                                             PublicMastodonActivityProvider publicProvider,
                                             IMemoryCache cache,
                                             ILoggerFactory loggerFactory) : IOwnedActivityProviderFactory
{
    public string IdentityProvider => MastodonEntryMapper.IdentityProviderToken;

    public OwnedActivityProviderBase Create(string userId, Connection connection)
    {
        var proxyRoot = new Uri(configuration["Endpoints:ConnectedServices:Proxy"]!);

        var proxy = httpClientFactory.CreateClient();
        proxy.BaseAddress = new Uri(proxyRoot, $"/proxy/{IdentityProvider}/");
        proxy.MaxResponseContentBufferSize = FediverseRequestGuard.MaxResponseBytes;
        proxy.DefaultRequestHeaders.Add("X-User-Id", userId);
        proxy.DefaultRequestHeaders.Add("X-Connection-Id", connection.Id);

        return new MastodonActivityProvider(connection, proxy, resolver, publicProvider, cache,
            loggerFactory.CreateLogger<MastodonActivityProvider>());
    }
}
