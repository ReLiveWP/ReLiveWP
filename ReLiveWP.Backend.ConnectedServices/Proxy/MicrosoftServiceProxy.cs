using System.Net.Http.Headers;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class MicrosoftServiceProxy(IServiceProvider services)
    : GenericHostServiceProxy<MicrosoftOAuthProvider>(MicrosoftService.SERVICE_NAME, services)
{
    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);
        return Task.CompletedTask;
    }
}
