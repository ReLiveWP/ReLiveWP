using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class CardDavServiceProxy(IServiceProvider services)
    : DavServiceProxyBase(CardDav.SERVICE_NAME, services)
{
    public override Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
    {
        var serviceUrl = new Uri(service.ServiceUrl!);

        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme != Uri.UriSchemeHttps || !SharesRegistrableDomain(absolute, serviceUrl))
                throw new InvalidOperationException($"{absolute.Host} is not part of the linked CardDAV account.");

            return absolute;
        }

        return new($"{service.ServiceUrl!.TrimEnd('/')}/{path.TrimStart('/')}{context.Request.QueryString}");
    }

    private static bool SharesRegistrableDomain(Uri a, Uri b)
    {
        if (a.Host.Equals(b.Host, StringComparison.OrdinalIgnoreCase)) return true;

        var suffix = RegistrableDomain(b);
        return suffix.Length > 0 &&
               a.Host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase);
    }

    private static string RegistrableDomain(Uri uri)
    {
        var labels = uri.Host.Split('.');
        return labels.Length < 2 ? string.Empty : string.Join('.', labels[^2..]);
    }
}
