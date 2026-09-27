namespace ReLiveWP.ServiceDefaults.Outbound;

public static class ExternalRequestGuard
{
    public const long MaxResponseBytes = 512 * 1024;
    public const int MaxRedirects = 3;

    private static readonly string[] PrivateSuffixes =
        [".localhost", ".local", ".internal", ".home.arpa", ".lan", ".intranet", ".corp"];

    public static HttpClient CreateGuardedClient(IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName);
        client.MaxResponseContentBufferSize = MaxResponseBytes;
        return client;
    }

    public static bool IsAcceptableUri(Uri? uri)
        => uri is { IsAbsoluteUri: true } &&
           uri.Scheme == Uri.UriSchemeHttps &&
           uri.IsDefaultPort &&
           uri.HostNameType == UriHostNameType.Dns &&
           string.IsNullOrEmpty(uri.UserInfo) &&
           IsPublicDnsName(uri.IdnHost);

    // the guarded handler never follows redirects, so every hop gets the same checks as the first request here
    public static Task<HttpResponseMessage?> GetFollowingRedirectsAsync(
        HttpClient http, Uri url, IEnumerable<string> accept, CancellationToken ct = default)
    {
        return GetFollowingRedirectsAsync(http, url, accept, HttpCompletionOption.ResponseContentRead, ct);
    }

    public static async Task<HttpResponseMessage?> GetFollowingRedirectsAsync(
        HttpClient http, Uri url, IEnumerable<string> accept, HttpCompletionOption completion, CancellationToken ct = default)
    {
        if (!IsAcceptableUri(url))
            return null;

        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var mediaType in accept)
                request.Headers.Accept.ParseAdd(mediaType);

            var response = await http.SendAsync(request, completion, ct);
            if ((int)response.StatusCode is not (>= 300 and < 400))
                return response;

            var location = response.Headers.Location;
            response.Dispose();

            if (location == null)
                return null;

            url = new Uri(url, location);
            if (!IsAcceptableUri(url))
                return null;
        }

        return null;
    }

    public static bool TryParseAcceptableUri(string? value, out Uri uri)
        => Uri.TryCreate(value, UriKind.Absolute, out uri!) && IsAcceptableUri(uri);

    public static bool TryCreateInstanceRoot(string? host, out Uri root)
    {
        root = null!;

        if (Uri.CheckHostName(host) != UriHostNameType.Dns)
            return false;

        root = new UriBuilder(Uri.UriSchemeHttps, host).Uri;
        return IsAcceptableUri(root);
    }

    public static bool IsOnInstance(Uri uri, Uri instance)
        => IsAcceptableUri(uri) &&
           string.Equals(uri.IdnHost, instance.IdnHost, StringComparison.OrdinalIgnoreCase);

    public static Uri GetInstanceRoot(Uri uri) => new UriBuilder(Uri.UriSchemeHttps, uri.IdnHost).Uri;

    private static bool IsPublicDnsName(string host)
        => host.Contains('.') &&
           !host.EndsWith('.') &&
           !PrivateSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}
