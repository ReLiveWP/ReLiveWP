using ReLiveWP.Backend.ConnectedServices.Providers;
using GoogleService = ReLiveWP.Backend.ConnectedServices.Providers.Google;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class GoogleServiceProxy(IServiceProvider services)
    : GenericHostServiceProxy<GoogleOAuthProvider>(GoogleService.SERVICE_NAME, services);
