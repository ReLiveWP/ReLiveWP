using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public abstract class GenericHostServiceProxy<T>(string serviceId, IServiceProvider services)
    : ConnectedServiceProxyBase<T>(serviceId, services) where T : IOAuthProvider
{
    public override Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
        => new Uri($"https://{path}{context.Request.QueryString}");
}
