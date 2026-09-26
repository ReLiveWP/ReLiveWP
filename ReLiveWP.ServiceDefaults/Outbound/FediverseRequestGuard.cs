namespace ReLiveWP.ServiceDefaults.Outbound;

public static class FediverseRequestGuard
{
    public const long MaxResponseBytes = 512 * 1024;

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
