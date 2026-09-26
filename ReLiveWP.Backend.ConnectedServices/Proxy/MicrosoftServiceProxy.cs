using ReLiveWP.Backend.ConnectedServices.Providers;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.Providers.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class MicrosoftServiceProxy(IServiceProvider services)
    : GenericHostServiceProxy<MicrosoftOAuthProvider>(MicrosoftService.SERVICE_NAME, services);
