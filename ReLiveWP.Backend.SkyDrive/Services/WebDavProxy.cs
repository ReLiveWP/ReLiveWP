using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.SkyDrive.Services;

public sealed class WebDavProxy(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public const string ServiceName = "webdav";

    private readonly string proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');

    public string Url(string path) => $"{proxyBase}/proxy/{ServiceName}/{DavPath.Encode(path)}";

    public static Dictionary<string, string> Credentials(string userId, string connectionId) => new()
    {
        ["X-Connection-ID"] = connectionId,
        ["X-User-ID"] = userId,
    };

    public DavClient CreateClient(string userId, string connectionId)
    {
        var client = httpClientFactory.CreateClient();

        foreach (var (name, value) in Credentials(userId, connectionId))
            client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);

        return new DavClient(client);
    }
}
