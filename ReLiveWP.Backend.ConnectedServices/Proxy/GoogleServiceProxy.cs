using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using GoogleService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Google;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class GoogleServiceProxy(IServiceProvider services)
    : GenericHostServiceProxy<GoogleOAuthProvider>(GoogleService.SERVICE_NAME, services);
