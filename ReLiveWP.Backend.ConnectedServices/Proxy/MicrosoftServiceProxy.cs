using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class MicrosoftServiceProxy(IServiceProvider services)
    : GenericHostServiceProxy<MicrosoftOAuthProvider>(MicrosoftService.SERVICE_NAME, services);
