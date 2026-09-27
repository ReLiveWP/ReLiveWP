using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class GooglePhotosProxyClient(IHttpClientFactory httpClientFactory,
                                    IConfiguration configuration,
                                    IMemoryCache cache,
                                    ILogger<GooglePhotosProxyClient> logger) : IPhotoSyncProxyClient
{
    public const string ServiceName = "google";
    private const string Host = "photoslibrary.googleapis.com";
    private const int PageSize = 100;
    private const int MaxItems = 150;
    private const int MaxPages = 50;

    private static readonly TimeSpan ListBudget = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan BaseUrlLifetime = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan ListLifetime = TimeSpan.FromSeconds(30);

    public string ServiceId => ServiceName;

    public async Task<string> EnsureAlbumAsync(string userId, string connectionId, string title, CancellationToken ct = default)
    {
        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');
        using var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/albums")
        {
            Content = JsonContent.Create(new { album = new { title } })
        };
        request.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        request.Headers.TryAddWithoutValidation("X-User-ID", userId);

        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Photos albums.create failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Google Photos album has no id.");
    }

    public Task<ProviderUploadTarget> BeginUploadAsync(string userId, string connectionId, string albumId,
                                                       PhotoUploadInfo photo, CancellationToken ct = default)
    {
        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');

        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/octet-stream",
            ["X-Goog-Upload-Protocol"] = "raw",
            ["X-Goog-Upload-File-Name"] = photo.FileName,
            ["X-Connection-ID"] = connectionId,
            ["X-User-ID"] = userId,
        };

        return Task.FromResult(new ProviderUploadTarget(
            "POST", $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/uploads", headers, 0));
    }

    public async Task<ProviderUploadResult> CompleteUploadAsync(string userId, string connectionId, string albumId,
                                                                PhotoUploadInfo photo, string responseBody, CancellationToken ct = default)
    {
        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');
        using var client = httpClientFactory.CreateClient();

        var uploadToken = responseBody.Trim();
        if (string.IsNullOrEmpty(uploadToken))
            throw new InvalidOperationException("Google Photos upload returned no token.");

        var batchBody = new
        {
            albumId,
            newMediaItems = new[]
            {
                new
                {
                    description = photo.Description ?? "",
                    simpleMediaItem = new
                    {
                        fileName = photo.FileName,
                        uploadToken
                    }
                }
            }
        };

        using var batchRequest = new HttpRequestMessage(HttpMethod.Post, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/mediaItems:batchCreate")
        {
            Content = JsonContent.Create(batchBody)
        };
        batchRequest.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        batchRequest.Headers.TryAddWithoutValidation("X-User-ID", userId);

        using var batchResponse = await client.SendAsync(batchRequest, ct);
        var json = await batchResponse.Content.ReadAsStringAsync(ct);
        if (!batchResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Photos batchCreate failed ({(int)batchResponse.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("newMediaItemResults")[0];

        if (!result.TryGetProperty("mediaItem", out var mediaItem))
        {
            var status = result.TryGetProperty("status", out var s) ? s.ToString() : "unknown";
            throw new InvalidOperationException($"Google Photos batchCreate returned no media item: {status}");
        }

        var id = mediaItem.GetProperty("id").GetString()!;
        var url = mediaItem.TryGetProperty("productUrl", out var p) ? p.GetString() : null;

        cache.Remove(ListCacheKey(connectionId, albumId));

        return new ProviderUploadResult(id, url);
    }

    public async Task<IReadOnlyList<ProviderPhoto>> ListAsync(string userId, string connectionId, string albumId, CancellationToken ct = default)
    {
        var cacheKey = ListCacheKey(connectionId, albumId);
        if (cache.TryGetValue<IReadOnlyList<ProviderPhoto>>(cacheKey, out var cached) && cached != null)
            return cached;

        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');
        using var client = httpClientFactory.CreateClient();

        var items = new List<ProviderPhoto>();
        var seenTokens = new HashSet<string>();
        string? pageToken = null;

        for (var page = 0; page < MaxPages; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/mediaItems:search")
            {
                Content = JsonContent.Create(string.IsNullOrEmpty(pageToken)
                    ? new { albumId, pageSize = PageSize, orderBy = "MediaMetadata.creation_time desc" }
                    : (object)new { albumId, pageSize = PageSize, pageToken, orderBy = "MediaMetadata.creation_time desc" })
            };
            request.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
            request.Headers.TryAddWithoutValidation("X-User-ID", userId);

            using var response = await client.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google Photos mediaItems.search failed ({(int)response.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var pageCount = 0;
            if (root.TryGetProperty("mediaItems", out var mediaItems))
            {
                foreach (var item in mediaItems.EnumerateArray())
                {
                    pageCount++;

                    var photo = ReadMediaItem(item);
                    if (photo == null)
                        continue;

                    items.Add(photo);

                    if (item.TryGetProperty("baseUrl", out var baseUrl) && baseUrl.GetString() is { Length: > 0 } mediaUrl)
                        CacheBaseUrl(connectionId, photo.ItemId, mediaUrl, photo.ContentType);
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null;

            logger.LogInformation("Google Photos album {AlbumId} page {Page}: {PageCount} items, {Total} total, nextPageToken {HasToken}",
                                  albumId, page, pageCount, items.Count, string.IsNullOrEmpty(pageToken) ? "absent" : "present");

            if (string.IsNullOrEmpty(pageToken) || items.Count >= MaxItems)
                break;

            if (!seenTokens.Add(pageToken))
                break;
        }

        cache.Set(cacheKey, (IReadOnlyList<ProviderPhoto>)items, ListLifetime);
        return items;
    }

    private static string ListCacheKey(string connectionId, string albumId) => $"google:list:{connectionId}:{albumId}";

    public async Task<ProviderContentLocation?> ResolveContentAsync(string userId, string connectionId, string itemId,
                                                                    int maxSize, bool refresh, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        if (refresh)
            cache.Remove(BaseUrlCacheKey(connectionId, itemId));

        var baseUrl = await ResolveBaseUrlAsync(client, userId, connectionId, itemId, ct);
        var sizedUrl = maxSize > 0 ? $"{baseUrl.Url}=w{maxSize}-h{maxSize}" : $"{baseUrl.Url}=d";

        return new ProviderContentLocation(sizedUrl, new Dictionary<string, string>(), baseUrl.ContentType, 0, null);
    }

    private async Task<CachedBaseUrl> ResolveBaseUrlAsync(HttpClient client, string userId, string connectionId, string itemId, CancellationToken ct)
    {
        var cacheKey = BaseUrlCacheKey(connectionId, itemId);
        if (cache.TryGetValue<CachedBaseUrl>(cacheKey, out var cached) && cached != null)
            return cached;

        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/mediaItems/{itemId}");
        request.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        request.Headers.TryAddWithoutValidation("X-User-ID", userId);

        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Photos mediaItems.get failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var url = doc.RootElement.GetProperty("baseUrl").GetString()
            ?? throw new InvalidOperationException("Google Photos media item has no baseUrl.");
        var mimeType = doc.RootElement.TryGetProperty("mimeType", out var m) ? m.GetString() : null;

        var value = new CachedBaseUrl(url, mimeType ?? "image/jpeg");
        cache.Set(cacheKey, value, BaseUrlLifetime);
        return value;
    }

    private static string BaseUrlCacheKey(string connectionId, string itemId) => $"google:baseurl:{connectionId}:{itemId}";

    public void CacheBaseUrl(string connectionId, string itemId, string baseUrl, string contentType)
        => cache.Set(BaseUrlCacheKey(connectionId, itemId), new CachedBaseUrl(baseUrl, contentType), BaseUrlLifetime);

    private static ProviderPhoto? ReadMediaItem(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return null;

        var mimeType = item.TryGetProperty("mimeType", out var mt) ? mt.GetString() ?? "" : "";
        var fileName = item.TryGetProperty("filename", out var fn) ? fn.GetString() ?? "" : "";
        var description = item.TryGetProperty("description", out var d) ? d.GetString() : null;

        var created = DateTimeOffset.UtcNow;
        var width = 0;
        var height = 0;

        if (item.TryGetProperty("mediaMetadata", out var meta))
        {
            if (meta.TryGetProperty("creationTime", out var ct) &&
                DateTimeOffset.TryParse(ct.GetString(), out var parsed))
                created = parsed;

            if (meta.TryGetProperty("width", out var w) && int.TryParse(w.GetString(), out var parsedWidth))
                width = parsedWidth;

            if (meta.TryGetProperty("height", out var h) && int.TryParse(h.GetString(), out var parsedHeight))
                height = parsedHeight;
        }

        return new ProviderPhoto(id, fileName, mimeType, description, created, width, height,
                                 mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
    }

    private record CachedBaseUrl(string Url, string ContentType);
}
