using System.Net;
using Microsoft.Extensions.Logging;

namespace ReLiveWP.ServiceDefaults.Media;

public sealed class MediaProxyClient(HttpClient http, MediaProxyUrlSigner signer, ILogger<MediaProxyClient> logger)
{
    public async Task<HttpResponseMessage?> FetchImageAsync(Uri source, MediaSize size, CancellationToken ct = default)
    {
        var path = signer.SignPath(source, size);
        if (path == null)
        {
            logger.LogWarning("Can't proxy {Source}, no proxy key or the proxy would refuse it", source);
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Media proxy unavailable for {Source}", source);
            return new HttpResponseMessage(HttpStatusCode.BadGateway);
        }
    }
}
