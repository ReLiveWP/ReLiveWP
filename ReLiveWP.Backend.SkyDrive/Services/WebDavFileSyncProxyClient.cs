using System.Net;
using ReLiveWP.Dav;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class WebDavFileSyncProxyClient(WebDavProxy proxy,
                                       ILogger<WebDavFileSyncProxyClient> logger) : IFileSyncProxyClient
{
    private const int MaxEntries = 5000;
    private const int MaxDepth = 16;

    private static readonly string ItemBody = DavBody.Propfind(
        DavProps.ResourceType, DavProps.GetContentType, DavProps.GetContentLength,
        DavProps.GetLastModified, DavProps.GetETag);

    public string ServiceId => WebDavProxy.ServiceName;

    // plain WebDAV has no change feed, so SkyDocs falls back to a recursive listing
    public bool SupportsDelta => false;

    public async Task<IReadOnlyList<ProviderLibrary>> ListLibrariesAsync(string userId, string connectionId,
                                                                         CancellationToken ct = default)
    {
        var entries = await ListChildrenAsync(userId, connectionId, "", recursive: false, ct);

        return [.. entries
            .Where(e => e.IsFolder)
            .Select(e => new ProviderLibrary(e.Path, e.Name, e.ResourceId, e.ReadOnly, e.Modified))];
    }

    public async Task<ProviderEntry?> GetItemAsync(string userId, string connectionId, string path,
                                                   CancellationToken ct = default)
    {
        using var dav = proxy.CreateClient(userId, connectionId);

        var listing = await PropfindAsync(dav, path, depth: "0", ct);
        if (listing is null)
            return null;

        var self = Anchor(listing);
        if (self is null)
            return null;

        var trimmed = path.Trim('/');

        return ToEntry(self, trimmed.Length == 0 ? "" : trimmed[(trimmed.LastIndexOf('/') + 1)..], trimmed);
    }

    public async Task<IReadOnlyList<ProviderEntry>> ListChildrenAsync(string userId, string connectionId, string path,
                                                                      bool recursive, CancellationToken ct = default)
    {
        using var dav = proxy.CreateClient(userId, connectionId);

        var items = new List<ProviderEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        await CollectAsync(dav, path, recursive, depth: 0, items, seen, ct);

        return items;
    }

    public async Task<ProviderChangeSet> GetChangesAsync(string userId, string connectionId, string path, string? cursor,
                                                         CancellationToken ct = default)
        => new(await ListChildrenAsync(userId, connectionId, path, recursive: true, ct), [], null);

    public async Task<ProviderContentLocation?> GetContentLocationAsync(string userId, string connectionId, string path,
                                                                        CancellationToken ct = default)
    {
        using var dav = proxy.CreateClient(userId, connectionId);

        var listing = await PropfindAsync(dav, path, depth: "0", ct);
        if (listing is null || Anchor(listing) is not { } self || self.IsCollection)
            return null;

        return new ProviderContentLocation(
            proxy.Url(path.Trim('/')),
            WebDavProxy.Credentials(userId, connectionId),
            ResolveContentType(self.ContentType, path),
            self.Length,
            self.ETag);
    }

    private async Task CollectAsync(DavClient dav, string path, bool recursive, int depth,
                                    List<ProviderEntry> items, HashSet<string> seen, CancellationToken ct)
    {
        if (depth >= MaxDepth || items.Count >= MaxEntries)
            return;

        var listing = await PropfindAsync(dav, path, depth: "1", ct);
        if (listing is null)
            return;

        var folders = new List<string>();

        foreach (var entry in Children(listing, path))
        {
            if (!seen.Add(entry.Path))
                continue;

            items.Add(entry);

            if (items.Count >= MaxEntries)
            {
                logger.LogWarning("WebDAV listing of {Path} hit the {Max} item cap, stopping short", path, MaxEntries);
                return;
            }

            if (recursive && entry.IsFolder)
                folders.Add(entry.Path);
        }

        foreach (var folder in folders)
            await CollectAsync(dav, folder, recursive, depth + 1, items, seen, ct);
    }

    // Hrefs come back rooted at the server, and the share root is not something this client knows.
    // The collection we asked for is in the listing too, so it supplies the prefix to strip.
    internal static IReadOnlyList<ProviderEntry> Children(DavMultiStatus listing, string requestPath)
    {
        if (Anchor(listing) is not { } self)
            return [];

        var prefix = self.Path.TrimEnd('/');
        var entries = new List<ProviderEntry>();

        foreach (var response in listing.Responses)
        {
            if (ReferenceEquals(response, self) ||
                !response.Path.StartsWith(prefix + "/", StringComparison.Ordinal))
                continue;

            var name = response.Path[(prefix.Length + 1)..].Trim('/');
            if (name.Length == 0)
                continue;

            entries.Add(ToEntry(response, name[(name.LastIndexOf('/') + 1)..], Combine(requestPath, name)));
        }

        return entries;
    }

    private async Task<DavMultiStatus?> PropfindAsync(DavClient dav, string path, string depth, CancellationToken ct)
    {
        try
        {
            return await dav.PropfindAsync(proxy.Url(path.Trim('/')), ItemBody, depth, ct);
        }
        catch (DavException e) when (e.Status is (int)HttpStatusCode.NotFound or (int)HttpStatusCode.Gone)
        {
            return null;
        }
    }

    // PROPFIND answers with the collection alongside its members, and the collection is always the
    // shortest path in the set. Anchoring on it means we never need to know the share root.
    private static DavResponse? Anchor(DavMultiStatus listing)
        => listing.Responses.MinBy(r => r.Path.Length);

    private static string Combine(string parent, string name)
    {
        var trimmed = parent.Trim('/');
        return trimmed.Length == 0 ? name : $"{trimmed}/{name}";
    }

    private static ProviderEntry ToEntry(DavResponse response, string name, string path)
    {
        var isFolder = response.IsCollection;

        return new ProviderEntry(
            name,
            path,
            isFolder,
            isFolder ? 0 : response.Length,
            response.Modified,
            response.Modified,
            isFolder ? "" : ResolveContentType(response.ContentType, path),
            response.ETag,
            false,
            WebDavItemId.Encode(path),
            null);
    }

    // plenty of servers label everything application/octet-stream, so the extension is the fallback
    private static string ResolveContentType(string? reported, string path)
    {
        if (!string.IsNullOrWhiteSpace(reported))
        {
            var media = reported.Split(';')[0].Trim();

            if (media.Length > 0 && !media.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                return media;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".one" => "application/onenote",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream",
        };
    }
}
