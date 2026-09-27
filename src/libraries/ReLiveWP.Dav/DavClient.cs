using System.Net;
using System.Text;

namespace ReLiveWP.Dav;

// Speaks DAV over whatever HttpClient it is handed. It knows nothing about how that client reaches
// the server, so callers own auth, address policy and any proxying.
public class DavClient(HttpClient client, string? baseAddress = null) : IDisposable
{
    private const int MaxMessageBody = 512;

    private readonly string? baseAddress = baseAddress?.TrimEnd('/');

    public HttpClient Http { get; } = client;

    public void Dispose()
    {
        Http.Dispose();
        GC.SuppressFinalize(this);
    }

    // Paths are appended verbatim: callers decide whether their path is escaped.
    public string Resolve(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out _))
            return path;

        return baseAddress is null ? path : $"{baseAddress}/{path.TrimStart('/')}";
    }

    public Task<DavMultiStatus> PropfindAsync(string path, string body, string depth = "0",
                                              CancellationToken ct = default)
        => MultiStatusAsync(DavMethods.Propfind, path, body, depth, ct);

    public Task<DavMultiStatus> ReportAsync(string path, string body, string depth = "1",
                                            CancellationToken ct = default)
        => MultiStatusAsync(DavMethods.Report, path, body, depth, ct);

    public Task<HttpResponseMessage> MkColAsync(string path, CancellationToken ct = default)
        => SendAsync(Request(DavMethods.MkCol, path), HttpCompletionOption.ResponseContentRead, ct);

    public Task<HttpResponseMessage> HeadAsync(string path, CancellationToken ct = default)
        => SendAsync(Request(HttpMethod.Head, path), HttpCompletionOption.ResponseContentRead, ct);

    public HttpRequestMessage Request(HttpMethod method, string path) => new(method, Resolve(path));

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completion,
                                               CancellationToken ct = default)
        => Http.SendAsync(request, completion, ct);

    private async Task<DavMultiStatus> MultiStatusAsync(HttpMethod method, string path, string body, string depth,
                                                        CancellationToken ct)
    {
        using var request = Request(method, path);
        request.Content = new StringContent(body, Encoding.UTF8, "application/xml");
        request.Headers.TryAddWithoutValidation("Depth", depth);

        using var response = await SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        var xml = await response.Content.ReadAsStringAsync(ct);

        var status = (int)response.StatusCode;

        if (IsSyncTokenRejection(response.StatusCode, xml))
            throw new DavSyncTokenException($"the server rejected the sync token for {path}", status, xml);

        if (status != 207 && !response.IsSuccessStatusCode)
            throw new DavException($"{method} {path} failed ({status}): {Truncate(xml)}", status, xml);

        return DavMultiStatus.Parse(xml);
    }

    // 410 means the token aged out; the rest carry a valid-sync-token precondition in the body.
    private static bool IsSyncTokenRejection(HttpStatusCode status, string body)
        => status is HttpStatusCode.Gone ||
           (status is HttpStatusCode.Forbidden or HttpStatusCode.Conflict &&
            body.Contains("valid-sync-token", StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string value) => value.Length <= MaxMessageBody
        ? value
        : value[..MaxMessageBody] + "...";
}
