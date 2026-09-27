using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Utilities;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public sealed record MastodonMediaSources(Uri Full, Uri Preview);

// public and unlisted only, whoever asks, so nothing here is keyed by viewer or sent with a token
public class MastodonAlbumProvider(MastodonActorResolver resolver,
                                   IHttpClientFactory httpClientFactory,
                                   IMemoryCache cache,
                                   ILogger<MastodonAlbumProvider> logger) : SocialAlbumProviderBase
{
    private const int PageSize = 40;
    private const int MaxPages = 5;
    private const int MaxPhotos = 150;

    private static readonly TimeSpan IdentityLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MissLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MediaLifetime = TimeSpan.FromMinutes(30);

    public override string Provider => MastodonEntryMapper.IdentityProviderToken;

    public override bool IsValidExternalId(string externalId) => TryParseAlbumKey(externalId, out _, out _);

    public override bool IsValidMediaId(string mediaId) => TryParseMediaId(mediaId, out _, out _);

    public override string FileNameFor(string mediaId)
        => TryParseMediaId(mediaId, out _, out var attachmentId) ? $"{attachmentId}.jpg" : base.FileNameFor(mediaId);

    public static string ComposeAlbumKey(Uri instance, string accountId) => $"{instance.IdnHost}+{accountId}";

    public static string ComposeMediaId(string statusId, string attachmentId) => $"{statusId}.{attachmentId}";

    public static bool TryParseAlbumKey(string albumKey, out Uri instance, out string accountId)
    {
        instance = null!;
        accountId = "";

        var parts = albumKey.Split('+');
        if (parts.Length != 2 || !MastodonEntryMapper.IsInstanceId(parts[1]))
            return false;

        if (!ExternalRequestGuard.TryCreateInstanceRoot(parts[0], out var root) ||
            !string.Equals(root.IdnHost, parts[0], StringComparison.OrdinalIgnoreCase))
            return false;

        instance = root;
        accountId = parts[1];
        return true;
    }

    public static bool TryParseMediaId(string mediaId, out string statusId, out string attachmentId)
    {
        statusId = attachmentId = "";

        var parts = mediaId.Split('.');
        if (parts.Length != 2 || !MastodonEntryMapper.IsInstanceId(parts[0]) || !MastodonEntryMapper.IsInstanceId(parts[1]))
            return false;

        statusId = parts[0];
        attachmentId = parts[1];
        return true;
    }

    public override async Task<string?> GetAlbumKeyAsync(string identityId, CancellationToken ct = default)
    {
        if (!ExternalRequestGuard.TryParseAcceptableUri(identityId, out var actorUri))
            return null;

        var account = await resolver.FindAccountAsync(actorUri, ct);
        return account == null ? null : ComposeAlbumKey(account.Instance, account.Account.Id);
    }

    public override async Task<string?> FindIdentityAsync(string albumKey, CancellationToken ct = default)
    {
        if (!TryParseAlbumKey(albumKey, out var instance, out var accountId))
            return null;

        var normalisedKey = ComposeAlbumKey(instance, accountId);
        var cacheKey = $"mastodon:album-identity:{normalisedKey}";
        if (cache.TryGetValue<string?>(cacheKey, out var cached))
            return cached;

        string? identity;
        try
        {
            identity = await LookUpIdentityAsync(instance, accountId, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogInformation(ex, "could not look up who {AlbumKey} is", normalisedKey);
            return null;
        }

        cache.Set(cacheKey, identity, identity == null ? MissLifetime : IdentityLifetime);
        return identity;
    }

    // the instance names the actor, then the actor has to resolve back to this same account,
    // so a hostile instance only ever gets to vouch for its own accounts
    private async Task<string?> LookUpIdentityAsync(Uri instance, string accountId, CancellationToken ct)
    {
        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);

        var accountUrl = new Uri(instance, $"/api/v1/accounts/{accountId}");
        var account = await MastodonRequests.GetJsonAsync<MastodonAccount>(http, accountUrl, logger, ct);
        if (account == null || account.Id != accountId)
            return null;

        var claimedActor = await resolver.ResolveAuthorAsync(account, instance, ct);
        if (claimedActor == null)
            return null;

        var confirmed = await resolver.FindAccountAsync(claimedActor, ct);
        if (confirmed == null || ComposeAlbumKey(confirmed.Instance, confirmed.Account.Id) != ComposeAlbumKey(instance, accountId))
        {
            logger.LogWarning("{Instance} says account {AccountId} is {Actor}, which doesn't resolve back to it",
                              instance.IdnHost, accountId, claimedActor);
            return null;
        }

        return claimedActor.AbsoluteUri;
    }

    public override async Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(
        string userId, IEnumerable<Connection> connections, CancellationToken ct = default)
    {
        var albums = new List<SocialAlbum>();

        foreach (var connection in connections.Where(c => c.Service == Provider))
        {
            try
            {
                if (!ExternalRequestGuard.TryParseAcceptableUri(connection.UserId, out var actorUri))
                    continue;

                var account = await resolver.FindAccountAsync(actorUri, ct);
                if (account == null)
                {
                    logger.LogInformation("no album for {Connection}, {Actor} didn't resolve", connection.Id, actorUri);
                    continue;
                }

                albums.Add(new SocialAlbum(
                    SocialAlbumRef.ForAlbum(Provider, ComposeAlbumKey(account.Instance, account.Account.Id)),
                    TitleFor(account.AccountAddress)));
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
        if (cache.TryGetValue<SocialAlbumContents>(CacheKey(Provider, externalId, null), out var cached) && cached?.Handle != null)
            return cached.Handle;

        return await FindAccountAddressAsync(externalId, ct);
    }

    public override async Task<SocialAlbumContents> GetAlbumAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default)
    {
        if (!TryParseAlbumKey(externalId, out var instance, out var accountId))
            throw new InvalidOperationException($"'{externalId}' is not a usable album key.");

        var albumKey = ComposeAlbumKey(instance, accountId);
        var cacheKey = CacheKey(Provider, albumKey, null);
        if (cache.TryGetValue<SocialAlbumContents>(cacheKey, out var cached) && cached != null)
            return cached;

        var photos = await FetchPhotosAsync(instance, accountId, ct);
        var handle = await FindAccountAddressAsync(albumKey, ct);

        logger.LogInformation("Synthesised {Count} photos for {AlbumKey}", photos.Count, albumKey);

        var contents = new SocialAlbumContents(photos, handle);
        cache.Set(cacheKey, contents, FeedLifetime);
        return contents;
    }

    public override async Task<Uri?> ResolveMediaSourceAsync(string albumKey, string mediaId, MediaSize size,
                                                             CancellationToken ct = default)
    {
        if (!TryParseAlbumKey(albumKey, out var instance, out var accountId) ||
            !TryParseMediaId(mediaId, out var statusId, out var attachmentId))
            return null;

        var sources = await FindMediaSourcesAsync(instance, accountId, statusId, attachmentId, ct);
        if (sources == null)
            return null;

        return size == MediaSize.Full ? sources.Full : sources.Preview;
    }

    private async Task<List<SocialPhoto>> FetchPhotosAsync(Uri instance, string accountId, CancellationToken ct)
    {
        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);

        var photos = new List<SocialPhoto>();
        string? maxId = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var query = $"/api/v1/accounts/{accountId}/statuses?only_media=true&exclude_reblogs=true&exclude_replies=true&limit={PageSize}";
            if (maxId != null)
                query += $"&max_id={maxId}";

            var statuses = await MastodonRequests.GetJsonAsync<MastodonStatus[]>(http, new Uri(instance, query), logger, ct);
            if (statuses == null || statuses.Length == 0)
                break;

            foreach (var status in statuses)
                ReadStatus(status, instance, accountId, photos);

            maxId = statuses[^1].Id;
            if (!MastodonEntryMapper.IsInstanceId(maxId) || photos.Count >= MaxPhotos)
                break;
        }

        if (photos.Count > MaxPhotos)
            photos.RemoveRange(MaxPhotos, photos.Count - MaxPhotos);

        return photos;
    }

    private void ReadStatus(MastodonStatus status, Uri instance, string accountId, List<SocialPhoto> photos)
    {
        if (!IsAlbumStatus(status, accountId))
            return;

        var albumKey = ComposeAlbumKey(instance, accountId);
        var created = (status.CreatedAt ?? DateTimeOffset.UtcNow).UtcDateTime;
        var index = 0;

        foreach (var attachment in status.MediaAttachments ?? [])
        {
            if (!TryReadMediaSources(attachment, out var sources))
                continue;

            cache.Set(MediaCacheKey(instance, accountId, status.Id, attachment.Id!), sources, MediaLifetime);

            var mediaId = ComposeMediaId(status.Id, attachment.Id!);
            var dimensions = attachment.Meta?.Original;

            photos.Add(new SocialPhoto(
                ResourceRef: SocialAlbumRef.ForPhoto(Provider, albumKey, mediaId),
                FileName: FileNameFor(mediaId),
                Summary: string.IsNullOrWhiteSpace(attachment.Description) ? null : attachment.Description,
                Created: created.AddMilliseconds(-index++),
                Width: dimensions?.Width ?? 0,
                Height: dimensions?.Height ?? 0));
        }
    }

    private async Task<MastodonMediaSources?> FindMediaSourcesAsync(Uri instance, string accountId, string statusId,
                                                                   string attachmentId, CancellationToken ct)
    {
        var cacheKey = MediaCacheKey(instance, accountId, statusId, attachmentId);
        if (cache.TryGetValue<MastodonMediaSources>(cacheKey, out var cached) && cached != null)
            return cached;

        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);
        var statusUrl = new Uri(instance, $"/api/v1/statuses/{statusId}");

        var status = await MastodonRequests.GetJsonAsync<MastodonStatus>(http, statusUrl, logger, ct);
        if (status == null || status.Id != statusId || !IsAlbumStatus(status, accountId))
            return null;

        var attachment = status.MediaAttachments?.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null || !TryReadMediaSources(attachment, out var sources))
            return null;

        cache.Set(cacheKey, sources, MediaLifetime);
        return sources;
    }

    private async Task<string?> FindAccountAddressAsync(string albumKey, CancellationToken ct)
    {
        var identity = await FindIdentityAsync(albumKey, ct);
        if (identity == null || !ExternalRequestGuard.TryParseAcceptableUri(identity, out var actorUri))
            return null;

        var account = await resolver.FindAccountAsync(actorUri, ct);
        return account?.AccountAddress;
    }

    // the owner check is what stops a ref naming one account from serving another's photo off the same instance
    private static bool IsAlbumStatus(MastodonStatus status, string accountId)
        => status.Account?.Id == accountId &&
           MastodonEntryMapper.IsInstanceId(status.Id) &&
           status.Visibility is "public" or "unlisted" &&
           status.Reblog == null &&
           status.InReplyToId == null &&
           status.Sensitive != true;

    private static bool TryReadMediaSources(MastodonMediaAttachment attachment, out MastodonMediaSources sources)
    {
        sources = null!;

        if (attachment.Type != "image" || !MastodonEntryMapper.IsInstanceId(attachment.Id))
            return false;

        if (!ExternalRequestGuard.TryParseAcceptableUri(attachment.Url, out var full))
            return false;

        var preview = ExternalRequestGuard.TryParseAcceptableUri(attachment.PreviewUrl, out var thumbnail) ? thumbnail : full;
        sources = new MastodonMediaSources(full, preview);
        return true;
    }

    private static string MediaCacheKey(Uri instance, string accountId, string statusId, string attachmentId)
        => $"mastodon:media:{instance.IdnHost}:{accountId}:{statusId}:{attachmentId}";
}
