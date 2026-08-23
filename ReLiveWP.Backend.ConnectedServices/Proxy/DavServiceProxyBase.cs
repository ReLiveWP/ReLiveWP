using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public abstract class DavServiceProxyBase(string serviceId, string displayName, IServiceProvider services)
    : ConnectedServiceProxyBase(serviceId, services)
{
    private readonly ConnectionSecretProtector protector = services.GetRequiredService<ConnectionSecretProtector>();

    public override bool PreserveContentLength => true;

    public override Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
    {
        var serviceUrl = new Uri(service.ServiceUrl!);

        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme != Uri.UriSchemeHttps ||
                !absolute.Host.Equals(serviceUrl.Host, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{absolute.Host} is not part of the linked {displayName} account.");

            return absolute;
        }

        return new($"{service.ServiceUrl!.TrimEnd('/')}/{path.TrimStart('/')}{context.Request.QueryString}");
    }

    public override Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(HttpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName));

    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        if (service.EncryptedSecret is not { Length: > 0 } secret)
            throw new InvalidOperationException($"{ServiceId} connection {service.Id} has no stored credentials.");

        request.Headers.Authorization =
            DavCredentials.BasicHeader(service.ServiceProfile.Username, protector.Unprotect(secret));

        return Task.CompletedTask;
    }
}
