using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class OneDrivePhotoSyncProxyClient(IHttpClientFactory httpClientFactory,
                                          IConfiguration configuration,
                                          IMemoryCache cache,
                                          ILogger<OneDrivePhotoSyncProxyClient> logger) : IPhotoSyncProxyClient
{
    public const string ServiceName = "microsoft";
    private const string Host = "graph.microsoft.com";
    private const int PageSize = 200;
    private const int MaxItems = 5000;
    private const int MaxPages = 50;
    private const int UploadFragment = 60 * 1024 * 1024;

    private static readonly TimeSpan ListBudget = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ThumbnailLifetime = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ListLifetime = TimeSpan.FromSeconds(30);

    public string ServiceId => ServiceName;

    private string Graph(string path)
        => $"{configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/')}/proxy/{ServiceName}/{Host}/{path}";

    private static HttpRequestMessage Request(HttpMethod method, string url, string userId, string connectionId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        request.Headers.TryAddWithoutValidation("X-User-ID", userId);
        return request;
    }

    public async Task<string> EnsureAlbumAsync(string userId, string connectionId, string title, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        var parentId = await ResolvePicturesFolderAsync(client, userId, connectionId, ct);

        var existing = await ReadItemIdAsync(client, userId, connectionId,
            $"v1.0/me/drive/items/{parentId}:/{Uri.EscapeDataString(title)}?$select=id", ct);
        if (existing != null)
            return existing;

        var created = await CreateFolderAsync(client, userId, connectionId, parentId, title, ct);
        if (created != null)
            return created;

        return await ReadItemIdAsync(client, userId, connectionId,
                   $"v1.0/me/drive/items/{parentId}:/{Uri.EscapeDataString(title)}?$select=id", ct)
               ?? throw new InvalidOperationException($"Could not create OneDrive album '{title}'.");
    }

    private async Task<string> ResolvePicturesFolderAsync(HttpClient client, string userId, string connectionId, CancellationToken ct)
    {
        var special = await ReadItemIdAsync(client, userId, connectionId, "v1.0/me/drive/special/photos?$select=id", ct);
        if (special != null)
            return special;

        var pictures = await ReadItemIdAsync(client, userId, connectionId, "v1.0/me/drive/root:/Pictures?$select=id", ct);
        if (pictures != null)
            return pictures;

        var root = await ReadItemIdAsync(client, userId, connectionId, "v1.0/me/drive/root?$select=id", ct)
                   ?? throw new InvalidOperationException("OneDrive drive root is unreachable.");

        return await CreateFolderAsync(client, userId, connectionId, root, "Pictures", ct)
               ?? await ReadItemIdAsync(client, userId, connectionId, "v1.0/me/drive/root:/Pictures?$select=id", ct)
               ?? throw new InvalidOperationException("Could not resolve a OneDrive Pictures folder.");
    }

    private async Task<string?> ReadItemIdAsync(HttpClient client, string userId, string connectionId, string path, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, Graph(path), userId, connectionId);
        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive item lookup failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private async Task<string?> CreateFolderAsync(HttpClient client, string userId, string connectionId,
                                                  string parentId, string name, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Post, Graph($"v1.0/me/drive/items/{parentId}/children"), userId, connectionId);
        request.Content = JsonContent.Create(new Dictionary<string, object>
        {
            ["name"] = name,
            ["folder"] = new { },
            ["@microsoft.graph.conflictBehavior"] = "fail",
        });

        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive folder create failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString();
    }

    public async Task<ProviderUploadTarget> BeginUploadAsync(string userId, string connectionId, string albumId,
                                                             PhotoUploadInfo photo, CancellationToken ct = default)
    {
        if (photo.Length == 0)
            throw new InvalidOperationException("Refusing to upload an empty photo.");

        using var client = httpClientFactory.CreateClient();

        using var sessionRequest = Request(HttpMethod.Post,
            Graph($"v1.0/me/drive/items/{albumId}:/{Uri.EscapeDataString(photo.FileName)}:/createUploadSession"),
            userId, connectionId);

        sessionRequest.Content = JsonContent.Create(new
        {
            item = new Dictionary<string, object>
            {
                ["@microsoft.graph.conflictBehavior"] = "rename",
            }
        });

        using var sessionResponse = await client.SendAsync(sessionRequest, ct);
        var sessionJson = await sessionResponse.Content.ReadAsStringAsync(ct);
        if (!sessionResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive createUploadSession failed ({(int)sessionResponse.StatusCode}): {sessionJson}");

        using var sessionDoc = JsonDocument.Parse(sessionJson);
        var uploadUrl = sessionDoc.RootElement.GetProperty("uploadUrl").GetString()
            ?? throw new InvalidOperationException("OneDrive upload session has no uploadUrl.");

        // the upload url carries its own auth, and the proxy forces chunked encoding which would drop
        // the Content-Length these range PUTs are documented to require, so this goes direct.
        return new ProviderUploadTarget("PUT", uploadUrl, new Dictionary<string, string>(), UploadFragment);
    }

    public Task<ProviderUploadResult> CompleteUploadAsync(string userId, string connectionId, string albumId,
                                                          PhotoUploadInfo photo, string responseBody, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var id = doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("OneDrive upload produced no item.");
        var url = doc.RootElement.TryGetProperty("webUrl", out var w) ? w.GetString() : null;

        cache.Remove(ListCacheKey(connectionId, albumId));
        return Task.FromResult(new ProviderUploadResult(id, url));
    }

    public async Task<IReadOnlyList<ProviderPhoto>> ListAsync(string userId, string connectionId, string albumId, CancellationToken ct = default)
    {
        var cacheKey = ListCacheKey(connectionId, albumId);
        if (cache.TryGetValue<IReadOnlyList<ProviderPhoto>>(cacheKey, out var cached) && cached != null)
            return cached;

        using var client = httpClientFactory.CreateClient();

        var items = new List<ProviderPhoto>();
        var seenLinks = new HashSet<string>();
        var deadline = DateTime.UtcNow + ListBudget;

        var url = Graph($"v1.0/me/drive/items/{albumId}/children?$top={PageSize}" +
                        "&$select=id,name,file,image,photo,video,createdDateTime,fileSystemInfo,@microsoft.graph.downloadUrl" +
                        "&$expand=thumbnails($select=large)");

        for (var page = 0; page < MaxPages; page++)
        {
            if (DateTime.UtcNow > deadline)
            {
                logger.LogWarning("Gave up listing OneDrive album after {Pages} pages / {Count} items, budget exhausted", page, items.Count);
                break;
            }

            using var request = Request(HttpMethod.Get, url, userId, connectionId);
            using var response = await client.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OneDrive children listing failed ({(int)response.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var pageCount = 0;
            if (root.TryGetProperty("value", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    pageCount++;

                    var photo = ReadDriveItem(item);
                    if (photo == null)
                        continue;

                    items.Add(photo);
                    CacheItemUrls(connectionId, photo, item);
                }
            }

            var nextLink = root.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;

            logger.LogInformation("OneDrive album {AlbumId} page {Page}: {PageCount} items, {Total} total, nextLink {HasLink}",
                                  albumId, page, pageCount, items.Count, string.IsNullOrEmpty(nextLink) ? "absent" : "present");

            if (string.IsNullOrEmpty(nextLink) || items.Count >= MaxItems)
                break;

            if (!seenLinks.Add(nextLink))
                break;

            url = RewriteNextLink(nextLink);
        }

        cache.Set(cacheKey, (IReadOnlyList<ProviderPhoto>)items, ListLifetime);
        return items;
    }

    // graph hands back an absolute url whose $skiptoken is opaque, so host, path and query are passed
    // through verbatim rather than parsed and rebuilt.
    private string RewriteNextLink(string nextLink)
    {
        var next = new Uri(nextLink);
        return $"{configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/')}/proxy/{ServiceName}/{next.Host}{next.AbsolutePath}{next.Query}";
    }

    private static string ListCacheKey(string connectionId, string albumId) => $"onedrive:list:{connectionId}:{albumId}";

    private static string ThumbnailCacheKey(string connectionId, string itemId, string size) => $"onedrive:thumb:{connectionId}:{itemId}:{size}";

    private static string DownloadCacheKey(string connectionId, string itemId) => $"onedrive:download:{connectionId}:{itemId}";

    private void CacheItemUrls(string connectionId, ProviderPhoto photo, JsonElement item)
    {
        if (item.TryGetProperty("@microsoft.graph.downloadUrl", out var download) && download.GetString() is { Length: > 0 } downloadUrl)
            cache.Set(DownloadCacheKey(connectionId, photo.ItemId), new CachedUrl(downloadUrl, photo.ContentType), DownloadUrlLifetime);

        if (TryReadLargeThumbnail(item, out var thumbnailUrl, out _, out _))
            cache.Set(ThumbnailCacheKey(connectionId, photo.ItemId, "large"), new CachedUrl(thumbnailUrl, "image/jpeg"), ThumbnailLifetime);
    }

    private static bool TryReadLargeThumbnail(JsonElement item, out string url, out int width, out int height)
    {
        url = "";
        width = 0;
        height = 0;

        if (!item.TryGetProperty("thumbnails", out var thumbnails) || thumbnails.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var set in thumbnails.EnumerateArray())
        {
            if (!set.TryGetProperty("large", out var large))
                continue;

            if (large.TryGetProperty("url", out var u) && u.GetString() is { Length: > 0 } value)
                url = value;

            if (large.TryGetProperty("width", out var w) && w.TryGetInt32(out var parsedWidth))
                width = parsedWidth;

            if (large.TryGetProperty("height", out var h) && h.TryGetInt32(out var parsedHeight))
                height = parsedHeight;

            return url.Length > 0;
        }

        return false;
    }

    private static string SizeSegment(int maxSize) => maxSize switch
    {
        96 => "small",
        176 => "medium",
        800 => "large",
        _ => $"c{maxSize}x{maxSize}",
    };

    public async Task<ProviderContentLocation?> ResolveContentAsync(string userId, string connectionId, string itemId,
                                                                    int maxSize, bool refresh, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        var cacheKey = maxSize > 0
            ? ThumbnailCacheKey(connectionId, itemId, SizeSegment(maxSize))
            : DownloadCacheKey(connectionId, itemId);

        if (refresh)
            cache.Remove(cacheKey);

        var target = await ResolveMediaUrlAsync(client, userId, connectionId, itemId, maxSize, cacheKey, ct);

        return new ProviderContentLocation(target.Url, new Dictionary<string, string>(), target.ContentType, 0, null);
    }

    private async Task<CachedUrl> ResolveMediaUrlAsync(HttpClient client, string userId, string connectionId,
                                                       string itemId, int maxSize, string cacheKey, CancellationToken ct)
    {
        if (cache.TryGetValue<CachedUrl>(cacheKey, out var cached) && cached != null)
            return cached;

        var path = maxSize > 0
            ? $"v1.0/me/drive/items/{itemId}/thumbnails/0/{SizeSegment(maxSize)}"
            : $"v1.0/me/drive/items/{itemId}?$select=id,file,@microsoft.graph.downloadUrl";

        using var request = Request(HttpMethod.Get, Graph(path), userId, connectionId);
        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive media url lookup failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? url;
        var contentType = "image/jpeg";

        if (maxSize > 0)
        {
            url = ReadThumbnailUrl(root);
        }
        else
        {
            url = root.TryGetProperty("@microsoft.graph.downloadUrl", out var d) ? d.GetString() : null;
            if (root.TryGetProperty("file", out var file) && file.TryGetProperty("mimeType", out var mime) &&
                mime.GetString() is { Length: > 0 } mimeType)
                contentType = mimeType;
        }

        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException($"OneDrive item {itemId} has no media url.");

        var value = new CachedUrl(url, contentType);
        cache.Set(cacheKey, value, maxSize > 0 ? ThumbnailLifetime : DownloadUrlLifetime);
        return value;
    }

    private static string? ReadThumbnailUrl(JsonElement root)
    {
        if (root.TryGetProperty("url", out var direct) && direct.GetString() is { Length: > 0 } url)
            return url;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object &&
                property.Value.TryGetProperty("url", out var nested) &&
                nested.GetString() is { Length: > 0 } nestedUrl)
                return nestedUrl;
        }

        return null;
    }

    private static ProviderPhoto? ReadDriveItem(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return null;

        if (!item.TryGetProperty("file", out var file))
            return null;

        var mimeType = file.TryGetProperty("mimeType", out var mt) ? mt.GetString() ?? "" : "";
        var isVideo = mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        if (!isVideo && !mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return null;

        var fileName = item.TryGetProperty("name", out var fn) ? fn.GetString() ?? "" : "";
        var description = item.TryGetProperty("description", out var d) ? d.GetString() : null;

        return new ProviderPhoto(id, fileName, mimeType, description,
                                 ReadCreated(item), ReadWidth(item), ReadHeight(item), isVideo);
    }

    private static DateTimeOffset ReadCreated(JsonElement item)
    {
        if (item.TryGetProperty("photo", out var photo) &&
            photo.TryGetProperty("takenDateTime", out var taken) &&
            DateTimeOffset.TryParse(taken.GetString(), out var takenAt))
            return takenAt;

        if (item.TryGetProperty("fileSystemInfo", out var fsInfo) &&
            fsInfo.TryGetProperty("createdDateTime", out var fsCreated) &&
            DateTimeOffset.TryParse(fsCreated.GetString(), out var fsCreatedAt))
            return fsCreatedAt;

        if (item.TryGetProperty("createdDateTime", out var created) &&
            DateTimeOffset.TryParse(created.GetString(), out var createdAt))
            return createdAt;

        return DateTimeOffset.UtcNow;
    }

    private static int ReadWidth(JsonElement item)
    {
        if (TryReadFacetDimension(item, "image", "width", out var width) ||
            TryReadFacetDimension(item, "video", "width", out width))
            return width;

        TryReadLargeThumbnail(item, out _, out var thumbnailWidth, out _);
        return thumbnailWidth;
    }

    private static int ReadHeight(JsonElement item)
    {
        if (TryReadFacetDimension(item, "image", "height", out var height) ||
            TryReadFacetDimension(item, "video", "height", out height))
            return height;

        TryReadLargeThumbnail(item, out _, out _, out var thumbnailHeight);
        return thumbnailHeight;
    }

    private static bool TryReadFacetDimension(JsonElement item, string facet, string property, out int value)
    {
        value = 0;
        return item.TryGetProperty(facet, out var f) &&
               f.TryGetProperty(property, out var p) &&
               p.TryGetInt32(out value) &&
               value > 0;
    }

    private record CachedUrl(string Url, string ContentType);
}
