using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public abstract class DavServiceProxyBase(string serviceId, IServiceProvider services)
    : ConnectedServiceProxyBase(serviceId, services)
{
    private readonly ConnectionSecretProtector protector = services.GetRequiredService<ConnectionSecretProtector>();

    public override bool PreserveContentLength => true;

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
