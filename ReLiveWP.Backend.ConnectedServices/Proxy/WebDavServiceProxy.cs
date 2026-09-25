using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class WebDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(WebDav.SERVICE_NAME, "WebDAV", services);
