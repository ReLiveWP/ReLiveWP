using ReLiveWP.Backend.ConnectedServices.Providers;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class CardDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(CardDav.SERVICE_NAME, "CardDAV", services)
{
    private const string ICloudDomainSuffix = ".icloud.com";

    private const string ICloudPhotoHost = "gateway.icloud.com";

    protected override bool IsAccountHost(Uri serviceUrl, Uri target)
    {
        if (base.IsAccountHost(serviceUrl, target))
            return true;

        return serviceUrl.Host.EndsWith(ICloudDomainSuffix, StringComparison.OrdinalIgnoreCase)
            && target.Host.Equals(ICloudPhotoHost, StringComparison.OrdinalIgnoreCase)
            && target.IsDefaultPort;
    }
}
