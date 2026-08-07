using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public record SocialAlbum(string ResourceId, string Title, int ItemCount);
public record SocialPhoto(string ResourceRef, string FileName, string? Summary, DateTime Created, int Width, int Height);

public class SocialAlbumProvider(IConfiguration configuration,
                                 IMemoryCache cache,
                                 ILoggerFactory loggerFactory)
{
    public const string Provider = "atproto";

    private const int PageSize = 100;
    private const int MaxPages = 5;

    private const int MaxPhotos = 150;

    private static readonly TimeSpan FeedLifetime = TimeSpan.FromMinutes(2);

    private readonly ILogger<SocialAlbumProvider> logger = loggerFactory.CreateLogger<SocialAlbumProvider>();

    public static string AlbumResourceId(string did) => $"{Provider}+{did}";

    public static string TitleFor(Connection connection) => $"@{connection.UserName.TrimStart('@')}'s photos";

    public static bool TryParseAlbumId(string resourceId, out string did)
    {
        did = "";
        if (!resourceId.StartsWith(Provider + "+", StringComparison.OrdinalIgnoreCase))
            return false;

        did = resourceId[(Provider.Length + 1)..];
        return did.StartsWith("did:", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParsePhotoRef(string resourceRef, out string did, out string cid)
    {
        did = cid = "";
        if (!resourceRef.StartsWith(Provider + "+", StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = resourceRef[(Provider.Length + 1)..];
        var split = rest.LastIndexOf('+');
        if (split <= 0 || split == rest.Length - 1)
            return false;

        did = rest[..split];
        cid = rest[(split + 1)..];
        return true;
    }

    public async Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(string userId, IEnumerable<Connection> connections, CancellationToken ct = default)
    {
        var albums = new List<SocialAlbum>();

        foreach (var connection in connections.Where(c => c.Service == Provider))
        {
            try
            {
                var photos = await GetPhotosAsync(userId, connection.UserId, connection, ct);
                albums.Add(new SocialAlbum(
                    AlbumResourceId(connection.UserId),
                    TitleFor(connection),
                    photos.Count));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to synthesise album for {Connection}", connection.Id);
            }
        }

        return albums;
    }

    public async Task<IReadOnlyList<SocialPhoto>> GetPhotosAsync(string userId, string did, Connection? connection, CancellationToken ct = default)
    {
        var cacheKey = $"social:{did}";
        if (cache.TryGetValue<IReadOnlyList<SocialPhoto>>(cacheKey, out var cached) && cached != null)
            return cached;

        var actor = ATDid.Create(did) ?? throw new InvalidOperationException($"'{did}' is not a usable DID.");
        using var protocol = CreateProtocol(userId, connection);

        var photos = new List<SocialPhoto>();
        string? cursor = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var result = await protocol.Feed.GetAuthorFeedAsync(
                actor, limit: PageSize, cursor: cursor, filter: "posts_with_media", cancellationToken: ct);

            var feed = result.HandleResult();
            if (feed?.Feed == null)
                break;

            foreach (var entry in feed.Feed)
                ReadPost(entry.Post, did, photos);

            cursor = feed.Cursor;
            if (string.IsNullOrEmpty(cursor) || photos.Count >= MaxPhotos)
                break;
        }

        if (photos.Count > MaxPhotos)
            photos.RemoveRange(MaxPhotos, photos.Count - MaxPhotos);

        logger.LogInformation("Synthesised {Count} photos for {Did}", photos.Count, did);

        cache.Set(cacheKey, (IReadOnlyList<SocialPhoto>)photos, FeedLifetime);
        return photos;
    }

    public static ContentLocation GetMediaLocation(string did, string cid, int maxSize)
    {
        var rendition = maxSize is > 0 and <= 320 ? "feed_thumbnail" : "feed_fullsize";
        var url = $"https://cdn.bsky.app/img/{rendition}/plain/{did}/{cid}@jpeg";

        return new ContentLocation(url, new Dictionary<string, string>(), "image/jpeg", null);
    }

    private ATProtocol CreateProtocol(string userId, Connection? connection)
    {
        var builder = new ATProtocolBuilder()
            .WithLogger(logger)
            .EnableAutoRenewSession(false)
            .WithServiceEndpointUponLogin(false);

        builder = connection != null
            ? builder.WithInstanceUrl(new Uri(new Uri(configuration["Endpoints:ConnectedServices:Proxy"]!), "/proxy/atproto"))
            : builder.WithInstanceUrl(new Uri("https://public.api.bsky.app"));

        var protocol = builder.Build();

        if (connection != null)
        {
            protocol.Client.DefaultRequestHeaders.Add("X-User-Id", userId);
            protocol.Client.DefaultRequestHeaders.Add("X-Connection-Id", connection.Id);
        }

        return protocol;
    }

    private static void ReadPost(PostView? post, string did, List<SocialPhoto> photos)
    {
        var images = post?.Embed switch
        {
            ViewImages view => view.Images,
            ViewRecordWithMedia { Media: ViewImages view } => view.Images,
            _ => null,
        };

        if (images == null || post == null)
            return;

        var created = (post.Record as Post)?.CreatedAt ?? post.IndexedAt ?? DateTime.UtcNow;
        var index = 0;

        foreach (var image in images)
        {
            var cid = ExtractCid(image.Fullsize ?? image.Thumb);
            if (string.IsNullOrEmpty(cid))
                continue;

            photos.Add(new SocialPhoto(
                ResourceRef: $"{Provider}+{did}+{cid}",
                FileName: $"{cid}.jpg",
                Summary: string.IsNullOrWhiteSpace(image.Alt) ? null : image.Alt,
                Created: created.ToUniversalTime().AddMilliseconds(-index++),
                Width: (int)(image.AspectRatio?.Width ?? 0),
                Height: (int)(image.AspectRatio?.Height ?? 0)));
        }
    }

    private static string? ExtractCid(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var lastSlash = url.LastIndexOf('/');
        if (lastSlash < 0)
            return null;

        var tail = url[(lastSlash + 1)..];
        var at = tail.IndexOf('@');
        return at > 0 ? tail[..at] : tail;
    }
}
