using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class CardDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(CardDav.SERVICE_NAME, "CardDAV", services);
