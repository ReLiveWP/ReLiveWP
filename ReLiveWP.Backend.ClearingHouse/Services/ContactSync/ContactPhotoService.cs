using ReLiveWP.ServiceDefaults.Contacts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactPhotoService(
    IHttpClientFactory httpClientFactory,
    ConnectedServicesProxy proxy,
    ILogger<ContactPhotoService> logger)
{
    public const int TileSize = 170;

    private const int MaxBytes = ContactContract.MaxPictureBytes;

    private const int MaxDownloadBytes = 8 * 1024 * 1024;

    private static readonly int[] QualitySteps = [82, 70, 58, 45];

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

        try
        {
            return Resize(source, contact.PhotoCrop);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "could not process the photo for {External}", contact.ExternalId);
            return null;
        }
    }

    private async Task<byte[]?> DownloadViaProxyAsync(
        string? url, string serviceId, SyncConnection connection, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            using var request = proxy.Request(HttpMethod.Get, serviceId, url, connection);
            using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

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

    private static byte[]? Resize(byte[] source, PhotoCrop? crop)
    {
        using var input = new MemoryStream(source, writable: false);
        using var image = Image.Load(input);

        image.Mutate(x => x.AutoOrient());

        if (crop is not null && Frame(crop, image.Width, image.Height) is { } frame)
            image.Mutate(x => x.Crop(frame));

        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(TileSize, TileSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            }));

        foreach (var quality in QualitySteps)
        {
            using var output = new MemoryStream();
            image.Save(output, new JpegEncoder { Quality = quality });

            if (output.Length <= MaxBytes)
                return output.ToArray();
        }

        return null;
    }

    internal static Rectangle? Frame(PhotoCrop crop, int width, int height)
    {
        var top = crop.OriginIsBottomLeft ? height - crop.Y - crop.Height : crop.Y;

        var frame = Rectangle.Intersect(
            new Rectangle(crop.X, top, crop.Width, crop.Height),
            new Rectangle(0, 0, width, height));

        return frame.Width > 0 && frame.Height > 0 ? frame : null;
    }
}
