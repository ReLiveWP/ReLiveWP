using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

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

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, HttpCompletionOption completion, CancellationToken ct) =>
        httpClientFactory.CreateClient().SendAsync(request, completion, ct);

    public static string Truncate(string value) => value.Length <= 512 ? value : value[..512] + "...";
}
