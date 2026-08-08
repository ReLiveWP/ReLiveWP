using System.Net.Http.Headers;
using System.Text;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public class WebDavServiceProxy(IServiceProvider services)
    : ConnectedServiceProxyBase(WebDav.SERVICE_NAME, services)
{
    private readonly ConnectionSecretProtector protector = services.GetRequiredService<ConnectionSecretProtector>();

    public override bool PreserveContentLength => true;

    public override Task<HttpClient> CreateHttpClientAsync(LiveConnectedService service)
        => Task.FromResult(HttpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName));

    public override Uri GetRequestUrl(LiveConnectedService service, HttpContext context, string path)
        => new($"{service.ServiceUrl!.TrimEnd('/')}/{path.TrimStart('/')}{context.Request.QueryString}");

    public override Task AddHeadersAsync(LiveConnectedService service, HttpRequestMessage request)
    {
        if (service.EncryptedSecret is not { Length: > 0 } secret)
            throw new InvalidOperationException($"WebDAV connection {service.Id} has no stored credentials.");

        var credentials = $"{service.ServiceProfile.Username}:{protector.Unprotect(secret)}";
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        return Task.CompletedTask;
    }
}
