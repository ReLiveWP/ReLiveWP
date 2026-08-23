using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class WebDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(WebDav.SERVICE_NAME, "WebDAV", services)
{
    public override Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
        => new($"{service.ServiceUrl!.TrimEnd('/')}/{path.TrimStart('/')}{context.Request.QueryString}");
}
