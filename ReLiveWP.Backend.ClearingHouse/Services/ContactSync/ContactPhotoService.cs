using System.Net;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.ServiceDefaults.Media;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactPhotoService(
    IHttpClientFactory httpClientFactory,
    ConnectedServicesProxy proxy,
    MediaPipelineClient pipeline,
    ILogger<ContactPhotoService> logger)
{
    public const int TileSize = MediaProfiles.ContactTileEdge;

    private const int MaxDownloadBytes = 8 * 1024 * 1024;

    private static readonly string[] AllowedPhotoHosts =
    [
        "googleusercontent.com",
        "google.com",
    ];

    public async Task<byte[]?> ResolveAsync(
        RemoteContact contact, SyncConnection? connection = null, CancellationToken ct = default)
    {
        var source = contact.PhotoData
            ?? (contact.PhotoServiceId is { } service && connection is not null
                ? await DownloadViaProxyAsync(contact.PhotoUrl, service, connection, ct)
                : await DownloadAsync(contact.PhotoUrl, ct));

        if (source is null) return null;

        var profile = ChooseTileProfile(contact.PhotoCrop);
        using var stream = new MemoryStream(source, writable: false);
        var result = await pipeline.ProcessImageAsync(stream, profile, ct);

        // failing the run keeps the delta token, so the batch is retried rather than written without photos
        if (result.IsUnavailable)
            throw new MediaPipelineUnavailableException();

        if (result.Jpeg is null)
            logger.LogWarning("could not process the photo for {External}: {Code}", contact.ExternalId, result.Rejection?.Code);

        return result.Jpeg;
    }

    internal static string ChooseTileProfile(PhotoCrop? crop)
    {
        if (crop is null)
            return MediaProfiles.ForContactTile(null, false);

        var rect = new MediaCrop(crop.X, crop.Y, crop.Width, crop.Height);
        return MediaProfiles.ForContactTile(rect, crop.OriginIsBottomLeft);
    }

    private async Task<byte[]?> DownloadViaProxyAsync(
        string? url, string serviceId, SyncConnection connection, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            using var request = proxy.Request(HttpMethod.Get, serviceId, url, connection);
            using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            // most contacts simply have no picture, and Graph says so with a 404 per contact
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("contact photo fetch returned {Status} for {Url}", (int)response.StatusCode, url);
                return null;
            }

            return await ReadCappedAsync(response, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "proxied contact photo fetch failed for {Url}", url);
            return null;
        }
    }

    private async Task<byte[]?> DownloadAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowed(uri))
        {
            logger.LogWarning("refusing to fetch a contact photo from {Url}", url);
            return null;
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("contact photo fetch returned {Status} for {Url}", (int)response.StatusCode, uri);
                return null;
            }

            return await ReadCappedAsync(response, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "contact photo fetch failed for {Url}", uri);
            return null;
        }
    }

    private static async Task<byte[]?> ReadCappedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength is > MaxDownloadBytes)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();

        var chunk = new byte[64 * 1024];
        int read;

        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxDownloadBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.Length == 0 ? null : buffer.ToArray();
    }

    private static bool IsAllowed(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps &&
        AllowedPhotoHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)
                                || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
}
