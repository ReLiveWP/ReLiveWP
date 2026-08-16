using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class BlueskyAlbumProvider(IConfiguration configuration,
                                  IMemoryCache cache,
                                  ILoggerFactory loggerFactory) : SocialAlbumProviderBase
{
    private const int PageSize = 100;
    private const int MaxPages = 5;

    private const int MaxPhotos = 150;

    private static readonly TimeSpan HandleLifetime = TimeSpan.FromMinutes(30);

    private readonly ILogger<BlueskyAlbumProvider> logger = loggerFactory.CreateLogger<BlueskyAlbumProvider>();

    public override string Provider => BlueskyEntryMapper.IdentityProviderToken;

    public override bool IsValidExternalId(string externalId) => ATDid.IsValid(externalId);

    public override bool IsValidMediaId(string mediaId) => mediaId.All(char.IsAsciiLetterOrDigit);

    public override async Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(
        string userId, IEnumerable<Connection> connections, CancellationToken ct = default)
    {
        var albums = new List<SocialAlbum>();

        foreach (var connection in connections.Where(c => c.Service == Provider))
        {
            try
            {
                // a listing carries no itemCount, so naming the album is all this needs
                var handle = await GetHandleAsync(userId, connection.UserId, connection, ct);
                albums.Add(new SocialAlbum(
                    SocialAlbumRef.Album(Provider, connection.UserId),
                    TitleFor(handle ?? connection.UserName)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to synthesise album for {Connection}", connection.Id);
            }
        }

        return albums;
    }

    public override async Task<string?> GetHandleAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default)
    {
        if (cache.TryGetValue<SocialAlbumContents>(CacheKey(Provider, externalId, connection), out var cached) && cached?.Handle != null)
            return cached.Handle;

        // a handle is the same whoever asks, so unlike the photos it does not split by view
        var key = $"social:handle:{externalId}";
        if (cache.TryGetValue<string>(key, out var handle) && handle != null)
            return handle;

        var actor = ATDid.Create(externalId);
        if (actor == null)
            return null;

        using var protocol = CreateProtocol(userId, connection);
        handle = await FetchHandleAsync(protocol, actor, ct);

        if (handle != null)
            cache.Set(key, handle, HandleLifetime);

        return handle;
    }

    public override async Task<SocialAlbumContents> GetAlbumAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(Provider, externalId, connection);
        if (cache.TryGetValue<SocialAlbumContents>(cacheKey, out var cached) && cached != null)
            return cached;

        var actor = ATDid.Create(externalId) ?? throw new InvalidOperationException($"'{externalId}' is not a usable DID.");
        using var protocol = CreateProtocol(userId, connection);

        var loggedOut = connection == null;
        var photos = new List<SocialPhoto>();
        string? handle = null;
        string? cursor = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var result = await protocol.Feed.GetAuthorFeedAsync(
                actor, limit: PageSize, cursor: cursor, filter: "posts_with_media", cancellationToken: ct);

            var feed = result.HandleResult();
            if (feed?.Feed == null)
                break;

            foreach (var entry in feed.Feed)
            {
                if (loggedOut && BlueskyEntryMapper.OptedOutOfLoggedOutViewing(entry.Post))
                    continue;

                handle ??= HandleOf(entry.Post, externalId);
                ReadPost(entry.Post, externalId, photos);
            }

            cursor = feed.Cursor;
            if (string.IsNullOrEmpty(cursor) || photos.Count >= MaxPhotos)
                break;
        }

        if (photos.Count > MaxPhotos)
            photos.RemoveRange(MaxPhotos, photos.Count - MaxPhotos);

        handle ??= await FetchHandleAsync(protocol, actor, ct);

        logger.LogInformation("Synthesised {Count} photos for {Did}", photos.Count, externalId);

        var contents = new SocialAlbumContents(photos, handle);
        cache.Set(cacheKey, contents, FeedLifetime);
        return contents;
    }

    private static string? HandleOf(PostView? post, string did) =>
        string.Equals(post?.Author?.Did?.ToString(), did, StringComparison.OrdinalIgnoreCase)
            ? post!.Author!.Handle?.ToString()
            : null;

    private async Task<string?> FetchHandleAsync(ATProtocol protocol, ATDid actor, CancellationToken ct)
    {
        try
        {
            var profile = (await protocol.Actor.GetProfileAsync(actor, cancellationToken: ct)).HandleResult();
            return profile?.Handle?.ToString();
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "no public profile available for {Did}", actor);
            return null;
        }
    }

    public override ContentLocation GetMediaLocation(string externalId, string mediaId, int maxSize)
    {
        var rendition = maxSize is > 0 and <= 320 ? "feed_thumbnail" : "feed_fullsize";
        var url = $"https://cdn.bsky.app/img/{rendition}/plain/{externalId}/{mediaId}@jpeg";

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

    private void ReadPost(PostView? post, string did, List<SocialPhoto> photos)
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
                ResourceRef: SocialAlbumRef.Photo(Provider, did, cid),
                FileName: FileNameFor(cid),
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
