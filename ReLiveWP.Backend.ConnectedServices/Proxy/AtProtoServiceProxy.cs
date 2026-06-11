using System.Net.Http.Headers;
using Duende.IdentityModel.OidcClient.DPoP;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class AtProtoServiceProxy(IServiceProvider services,
                                 IJWKProvider jwkProvider,
                                 IHttpMessageHandlerFactory handlerFactory)
    : ConnectedServiceProxyBase<AtProtoOAuthProvider>(AtProto.SERVICE_NAME, services)
{
    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", service.AccessToken);
        return Task.CompletedTask;
    }

    public override async Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
    {
        var key = await jwkProvider.GetJWKAsync(service.DPoPKeyId!);

        var innerHandler = handlerFactory.CreateHandler();
        var tokenHandler = new ProofTokenMessageHandler(key, innerHandler);
        var client = new HttpClient(tokenHandler);

        return client;
    }
}
