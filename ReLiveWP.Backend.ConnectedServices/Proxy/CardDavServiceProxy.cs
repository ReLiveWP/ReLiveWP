using System.Net.Http.Headers;
using System.Text;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class CardDavServiceProxy(IServiceProvider services)
    : ConnectedServiceProxyBase(CardDav.SERVICE_NAME, services)
{
    private readonly ConnectionSecretProtector protector = services.GetRequiredService<ConnectionSecretProtector>();

    public override bool PreserveContentLength => true;

    public override Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(HttpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName));

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

    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        if (service.EncryptedSecret is not { Length: > 0 } secret)
            throw new InvalidOperationException($"CardDAV connection {service.Id} has no stored credentials.");

        var credentials = $"{service.ServiceProfile.Username}:{protector.Unprotect(secret)}";
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        return Task.CompletedTask;
    }
}
