using System.Buffers;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Dav;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class WebDavPhotoSyncClient(WebDavProxy proxy,
                                   IMemoryCache cache,
                                   WebDavUploadStore uploads,
                                   ILogger<WebDavPhotoSyncClient> logger) : IPhotoSyncProxyClient
{
    private const int MaxItems = 5000;
    private const int MaxNameAttempts = 50;

    private static readonly TimeSpan ListLifetime = TimeSpan.FromSeconds(30);

    private static readonly string ListBody = DavBody.Propfind(
        DavProps.ResourceType, DavProps.GetContentType, DavProps.GetContentLength,
        DavProps.GetLastModified, DavProps.GetETag);

    public string ServiceId => WebDavProxy.ServiceName;

    public async Task<string> EnsureAlbumAsync(string userId, string connectionId, string title, CancellationToken ct = default)
    {
        var album = SanitiseSegment(title);

        using var dav = proxy.CreateClient(userId, connectionId);
        using var response = await dav.MkColAsync(proxy.Url(album), ct);

        // 405 is the collection already existing, which is the normal case after the first sync
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MethodNotAllowed)
            return album;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"WebDAV MKCOL '{album}' failed ({(int)response.StatusCode}): {body}");
    }

    public async Task<IReadOnlyList<ProviderPhoto>> ListAsync(string userId, string connectionId, string albumId,
                                                              CancellationToken ct = default)
    {
        var cacheKey = ListCacheKey(connectionId, albumId);
        if (cache.TryGetValue<IReadOnlyList<ProviderPhoto>>(cacheKey, out var cached) && cached != null)
            return cached;

        using var dav = proxy.CreateClient(userId, connectionId);

        DavMultiStatus listing;
        try
        {
            listing = await dav.PropfindAsync(proxy.Url(albumId), ListBody, depth: "1", ct);
        }
        catch (DavException e) when (e.Status == (int)HttpStatusCode.NotFound)
        {
            return [];
        }

        var items = new List<ProviderPhoto>();
        foreach (var entry in listing.Responses)
        {
            if (entry.IsCollection || items.Count >= MaxItems)
                continue;

            var relative = DavPath.RelativeTo(entry.Path, albumId);
            if (relative == null)
                continue;

            var contentType = ResolveContentType(entry.ContentType, relative);
            if (!IsMedia(contentType))
                continue;

            var path = $"{albumId.Trim('/')}/{relative}";

            items.Add(new ProviderPhoto(
                WebDavItemId.Encode(path),
                relative[(relative.LastIndexOf('/') + 1)..],
                contentType,
                null,
                entry.Modified,
                0,
                0,
                contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)));
        }

        logger.LogInformation("WebDAV album {AlbumId}: {Count} media item(s), {Skipped} unaddressable",
            albumId, items.Count, listing.Skipped);

        cache.Set(cacheKey, (IReadOnlyList<ProviderPhoto>)items, ListLifetime);
        return items;
    }

    public async Task<ProviderUploadTarget> BeginUploadAsync(string userId, string connectionId, string albumId,
                                                             PhotoUploadInfo photo, CancellationToken ct = default)
    {
        if (photo.Length == 0)
            throw new InvalidOperationException("Refusing to upload an empty photo.");

        var path = await ResolveFreeNameAsync(userId, connectionId, albumId, SanitiseSegment(photo.FileName), ct);
        await uploads.StashAsync(connectionId, albumId, photo.FileName, path);

        return new ProviderUploadTarget("PUT", proxy.Url(path), WebDavProxy.Credentials(userId, connectionId), 0);
    }

    public async Task<ProviderUploadResult> CompleteUploadAsync(string userId, string connectionId, string albumId,
                                                                PhotoUploadInfo photo, string responseBody,
                                                                CancellationToken ct = default)
    {
        var path = await uploads.TakeAsync(connectionId, albumId, photo.FileName)
                   ?? $"{albumId.Trim('/')}/{SanitiseSegment(photo.FileName)}";

        cache.Remove(ListCacheKey(connectionId, albumId));

        return new ProviderUploadResult(WebDavItemId.Encode(path), null);
    }

    public Task<ProviderContentLocation?> ResolveContentAsync(string userId, string connectionId, string itemId,
                                                              int maxSize, bool refresh, CancellationToken ct = default)
    {
        if (!WebDavItemId.TryDecode(itemId, out var path))
            return Task.FromResult<ProviderContentLocation?>(null);

        var contentType = ResolveContentType(null, path);

        // the share serves originals only, so anything sized has to be scaled downstream
        var resizeTo = maxSize > 0 && !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? maxSize
            : 0;

        return Task.FromResult<ProviderContentLocation?>(new ProviderContentLocation(
            proxy.Url(path), WebDavProxy.Credentials(userId, connectionId), contentType, 0, null, resizeTo));
    }

    private async Task<string> ResolveFreeNameAsync(string userId, string connectionId, string albumId,
                                                    string fileName, CancellationToken ct)
    {
        var album = albumId.Trim('/');
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        using var dav = proxy.CreateClient(userId, connectionId);

        for (var attempt = 0; attempt < MaxNameAttempts; attempt++)
        {
            var candidate = attempt == 0 ? fileName : $"{stem} ({attempt}){extension}";
            var path = $"{album}/{candidate}";

            using var response = await dav.HeadAsync(proxy.Url(path), ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return path;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WebDAV HEAD on {Path} returned {Status}, uploading anyway", path, (int)response.StatusCode);
                return path;
            }
        }

        return $"{album}/{Guid.NewGuid():N}{extension}";
    }

    private static string ListCacheKey(string connectionId, string albumId) => $"webdav:list:{connectionId}:{albumId}";

    private static readonly SearchValues<char> InvalidNameChars = SearchValues.Create("/\\#?%:*\"<>|");

    private static string SanitiseSegment(string name)
    {
        var cleaned = new string([.. name.Where(c => !char.IsControl(c) && !InvalidNameChars.Contains(c))]).Trim();

        if (cleaned is "" or "." or "..")
            cleaned = Guid.NewGuid().ToString("N");

        return cleaned.Length > 200 ? cleaned[..200] : cleaned;
    }

    private static bool IsMedia(string contentType)
        => contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
           contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    // plenty of servers label everything application/octet-stream, so the extension is the fallback
    private static string ResolveContentType(string? reported, string path)
    {
        if (!string.IsNullOrWhiteSpace(reported))
        {
            var media = reported.Split(';')[0].Trim();
            if (IsMedia(media))
                return media;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".jpe" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".heic" or ".heif" => "image/heic",
            ".tif" or ".tiff" => "image/tiff",
            ".mp4" or ".m4v" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream",
        };
    }
}
