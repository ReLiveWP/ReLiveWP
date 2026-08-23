using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public sealed class ConnectedServicesProxy(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly string _proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');

    public HttpRequestMessage Request(
        HttpMethod method, string serviceId, string path, SyncConnection connection)
    {
        var request = new HttpRequestMessage(method, $"{_proxyBase}/proxy/{serviceId}/{path.TrimStart('/')}");

        request.Headers.TryAddWithoutValidation("X-Connection-ID", connection.ConnectionId);
        request.Headers.TryAddWithoutValidation("X-User-ID", connection.UserId);

        return request;
    }

    public DavClient CreateDavClient(string serviceId, SyncConnection connection)
    {
        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Connection-ID", connection.ConnectionId);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-ID", connection.UserId);

        return new DavClient(client, $"{_proxyBase}/proxy/{serviceId}");
    }

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, HttpCompletionOption completion, CancellationToken ct) =>
        httpClientFactory.CreateClient().SendAsync(request, completion, ct);

    public static string Truncate(string value) => value.Length <= 512 ? value : value[..512] + "...";
}
