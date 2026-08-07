using System.Net;
using System.Text.Json;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class OneDriveFileSyncProxyClient(IHttpClientFactory httpClientFactory,
                                         IConfiguration configuration,
                                         ILogger<OneDriveFileSyncProxyClient> logger) : IFileSyncProxyClient
{
    public const string ServiceName = "microsoft";
    private const string Host = "graph.microsoft.com";
    private const string RootMarker = "/drive/root:";
    private const string ItemSelect = "id,name,size,eTag,file,folder,createdDateTime,lastModifiedDateTime,fileSystemInfo,parentReference,deleted";
    private const int PageSize = 200;
    private const int MaxPages = 50;

    public string ServiceId => ServiceName;

    public bool SupportsDelta => true;

    private string Graph(string path)
        => $"{configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/')}/proxy/{ServiceName}/{Host}/{path}";

    private string Proxied(string absoluteUrl)
    {
        var uri = new Uri(absoluteUrl);
        return $"{configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/')}/proxy/{ServiceName}/{uri.Host}{uri.AbsolutePath}{uri.Query}";
    }

    private static HttpRequestMessage Request(HttpMethod method, string url, string userId, string connectionId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        request.Headers.TryAddWithoutValidation("X-User-ID", userId);
        return request;
    }

    private static string ItemUrl(string path)
        => string.IsNullOrEmpty(path)
            ? "v1.0/me/drive/root"
            : $"v1.0/me/drive/root:/{EscapePath(path)}:";

    private static string EscapePath(string path)
        => string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

    private async Task<JsonDocument?> GetJsonAsync(HttpClient client, string url, string userId, string connectionId,
                                                   CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, url, userId, connectionId);
        using var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive request failed ({(int)response.StatusCode}): {json}");

        return JsonDocument.Parse(json);
    }

    public async Task<IReadOnlyList<ProviderLibrary>> ListLibrariesAsync(string userId, string connectionId,
                                                                         CancellationToken ct = default)
    {
        var entries = await ListChildrenAsync(userId, connectionId, "", false, ct);

        return [.. entries
            .Where(e => e.IsFolder)
            .Select(e => new ProviderLibrary(e.Path, e.Name, e.ResourceId, e.ReadOnly, e.Modified))];
    }

    public async Task<ProviderEntry?> GetItemAsync(string userId, string connectionId, string path,
                                                   CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        using var doc = await GetJsonAsync(client, Graph($"{ItemUrl(path)}?$select={ItemSelect}"), userId, connectionId, ct);
        if (doc == null)
            return null;

        if (string.IsNullOrEmpty(path))
            return new ProviderEntry("", "", true, 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "", null, false,
                                     doc.RootElement.TryGetProperty("id", out var rootId) ? rootId.GetString() ?? "" : "", null);

        return ReadEntry(doc.RootElement);
    }

    public async Task<IReadOnlyList<ProviderEntry>> ListChildrenAsync(string userId, string connectionId, string path,
                                                                      bool recursive, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        var items = new List<ProviderEntry>();
        await CollectChildrenAsync(client, userId, connectionId, path, recursive, items, ct);
        return items;
    }

    private async Task CollectChildrenAsync(HttpClient client, string userId, string connectionId, string path,
                                            bool recursive, List<ProviderEntry> items, CancellationToken ct)
    {
        var url = Graph($"{ItemUrl(path)}/children?$top={PageSize}&$select={ItemSelect}");
        var folders = new List<string>();

        for (var page = 0; page < MaxPages; page++)
        {
            using var doc = await GetJsonAsync(client, url, userId, connectionId, ct);
            if (doc == null)
                return;

            var root = doc.RootElement;
            if (root.TryGetProperty("value", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    var entry = ReadEntry(item);
                    if (entry == null)
                        continue;

                    items.Add(entry);
                    if (recursive && entry.IsFolder)
                        folders.Add(entry.Path);
                }
            }

            var nextLink = root.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;
            if (string.IsNullOrEmpty(nextLink))
                break;

            url = Proxied(nextLink);
        }

        foreach (var folder in folders)
            await CollectChildrenAsync(client, userId, connectionId, folder, true, items, ct);
    }

    public async Task<ProviderChangeSet> GetChangesAsync(string userId, string connectionId, string path, string? cursor,
                                                         CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        var url = string.IsNullOrEmpty(cursor)
            ? Graph($"{ItemUrl(path)}/delta?$select={ItemSelect}")
            : Proxied(cursor);

        var changed = new List<ProviderEntry>();
        var deleted = new List<string>();
        string? deltaLink = null;

        for (var page = 0; page < MaxPages; page++)
        {
            JsonDocument? doc;
            try
            {
                doc = await GetJsonAsync(client, url, userId, connectionId, ct);
            }
            catch (InvalidOperationException) when (!string.IsNullOrEmpty(cursor))
            {
                logger.LogInformation("OneDrive rejected the delta cursor for {Path}, restarting from empty", path);
                return new ProviderChangeSet([], [], null);
            }

            if (doc == null)
                return new ProviderChangeSet([], [], null);

            using (doc)
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("value", out var values))
                {
                    foreach (var item in values.EnumerateArray())
                    {
                        var itemPath = ReadPath(item);
                        if (string.IsNullOrEmpty(itemPath) || itemPath == path)
                            continue;

                        if (item.TryGetProperty("deleted", out _))
                        {
                            deleted.Add(itemPath);
                            continue;
                        }

                        var entry = ReadEntry(item);
                        if (entry != null)
                            changed.Add(entry);
                    }
                }

                var nextLink = root.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;
                deltaLink = root.TryGetProperty("@odata.deltaLink", out var delta) ? delta.GetString() : deltaLink;

                if (string.IsNullOrEmpty(nextLink))
                    break;

                url = Proxied(nextLink);
            }
        }

        return new ProviderChangeSet(changed, deleted, deltaLink);
    }

    public async Task<ProviderContentLocation?> GetContentLocationAsync(string userId, string connectionId, string path,
                                                                        CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        using var doc = await GetJsonAsync(client, Graph(ItemUrl(path)), userId, connectionId, ct);
        if (doc == null)
            return null;

        var root = doc.RootElement;
        if (root.TryGetProperty("folder", out _))
            return null;

        var contentType = root.TryGetProperty("file", out var file) && file.TryGetProperty("mimeType", out var mime)
            ? mime.GetString() ?? "application/octet-stream"
            : "application/octet-stream";

        var size = root.TryGetProperty("size", out var s) && s.TryGetInt64(out var parsedSize) ? parsedSize : 0;
        var etag = root.TryGetProperty("eTag", out var e) ? e.GetString() : null;

        var url = root.TryGetProperty("@microsoft.graph.downloadUrl", out var d) ? d.GetString() : null;
        if (!string.IsNullOrEmpty(url))
            return new ProviderContentLocation(url, new Dictionary<string, string>(), contentType, size, etag);

        var headers = new Dictionary<string, string>
        {
            ["X-Connection-ID"] = connectionId,
            ["X-User-ID"] = userId,
        };

        return new ProviderContentLocation(Graph($"{ItemUrl(path)}/content"), headers, contentType, size, etag);
    }

    private static ProviderEntry? ReadEntry(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            return null;

        var path = ReadPath(item);
        if (string.IsNullOrEmpty(path))
            return null;

        var isFolder = item.TryGetProperty("folder", out _);
        var size = item.TryGetProperty("size", out var s) && s.TryGetInt64(out var parsedSize) ? parsedSize : 0;
        var etag = item.TryGetProperty("eTag", out var e) ? e.GetString() : null;

        var contentType = !isFolder && item.TryGetProperty("file", out var file) &&
                          file.TryGetProperty("mimeType", out var mime)
            ? mime.GetString() ?? "application/octet-stream"
            : "";

        return new ProviderEntry(name, path, isFolder, isFolder ? 0 : size,
                                 ReadDate(item, "createdDateTime"), ReadDate(item, "lastModifiedDateTime"),
                                 contentType, etag, false, id, null);
    }

    private static string ReadPath(JsonElement item)
    {
        var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        if (string.IsNullOrEmpty(name))
            return "";

        if (!item.TryGetProperty("parentReference", out var parent) ||
            !parent.TryGetProperty("path", out var pathProp) ||
            pathProp.GetString() is not { Length: > 0 } parentPath)
            return name;

        var marker = parentPath.IndexOf(RootMarker, StringComparison.Ordinal);
        var relative = marker < 0 ? "" : parentPath[(marker + RootMarker.Length)..];
        relative = Uri.UnescapeDataString(relative).Trim('/');

        return string.IsNullOrEmpty(relative) ? name : $"{relative}/{name}";
    }

    private static DateTimeOffset ReadDate(JsonElement item, string property)
    {
        if (item.TryGetProperty("fileSystemInfo", out var fsInfo) &&
            fsInfo.TryGetProperty(property, out var fsValue) &&
            DateTimeOffset.TryParse(fsValue.GetString(), out var fsParsed))
            return fsParsed;

        if (item.TryGetProperty(property, out var value) &&
            DateTimeOffset.TryParse(value.GetString(), out var parsed))
            return parsed;

        return DateTimeOffset.UnixEpoch;
    }
}
