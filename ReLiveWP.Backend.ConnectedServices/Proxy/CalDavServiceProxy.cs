using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class CalDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(CalDav.SERVICE_NAME, "CalDAV", services);
