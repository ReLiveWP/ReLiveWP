using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Services;

public record ContactFeedSource(string Provider, string ExternalId, string? Handle = null);

public class ActivityProviderService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILoggerFactory loggerFactory,
    ConnectedServices.ConnectedServicesClient connectedServices,
    MailboxStore.MailboxStoreClient mailbox,
    IMemoryCache cache,
    ILogger<ActivityProviderService> logger)
{
    private static readonly TimeSpan SharedConnectionsLifetime = TimeSpan.FromSeconds(45);

    private const ulong BustedFlag = 0x80000000UL;
    private const uint SocialFeedCapability = 0x20;
    private const uint SocialPhotosCapability = 0x1000;

    private IReadOnlyList<PublicActivityProviderBase>? publicProviders;

    public IReadOnlyList<PublicActivityProviderBase> PublicProviders =>
        publicProviders ??= [new PublicBlueskyActivityProvider(loggerFactory, cache)];

    public async Task<OwnedActivityProviderBase?> GetOwnedProviderAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        var auth = context.User.Id()!;
        var servicesResponse = connectedServices.GetConnections(new ConnectionsRequest());

        List<OwnedActivityProviderBase> providers = [];
        await foreach (var connection in servicesResponse.ResponseStream.ReadAllAsync())
        {
            if (connection.Service == BlueskyEntryMapper.IdentityProviderToken && (connection.Flags & BustedFlag) == 0)
            {
                providers.Add(new BlueskyActivityProvider(auth, connection, configuration, loggerFactory));
            }
        }

        return new FeedCoalescingActivityProvider([.. providers]);
    }

    // replies are public, so a viewer with nothing linked still gets to read them
    public async Task<IReadOnlyList<ActivityProviderBase>> GetReplyProvidersAsync()
    {
        var owned = await GetOwnedProviderAsync();
        return owned == null ? PublicProviders : [owned];
    }

    public Task<IReadOnlyList<ContactFeedSource>> GetContactFeedSourcesAsync(
        long cid, string viewerUserId, CancellationToken ct = default)
        => GetContactSourcesAsync(cid, viewerUserId, SocialFeedCapability, ct);

    public Task<IReadOnlyList<ContactFeedSource>> GetContactPhotoSourcesAsync(
        long cid, string viewerUserId, CancellationToken ct = default)
        => GetContactSourcesAsync(cid, viewerUserId, SocialPhotosCapability, ct);

    // a did the viewer has no relationship with is not ours to fetch and hand over, even though the
    // appview would serve it to anyone
    public async Task<bool> IsServableDidAsync(
        string did, long? subjectCid, string viewerUserId, CancellationToken ct = default)
    {
        if (await OwnsDidAsync(did, ct))
            return true;

        if (await HasContactIdentityAsync(did, viewerUserId, ct))
            return true;

        if (subjectCid is not { } cid)
            return false;

        var sources = await GetContactPhotoSourcesAsync(cid, viewerUserId, ct);
        return sources.Any(s => string.Equals(s.ExternalId, did, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> OwnsDidAsync(string did, CancellationToken ct)
    {
        var call = connectedServices.GetConnections(new ConnectionsRequest(), cancellationToken: ct);
        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
        {
            if (connection.Service == BlueskyEntryMapper.IdentityProviderToken &&
                string.Equals(connection.UserId, did, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> HasContactIdentityAsync(string did, string viewerUserId, CancellationToken ct)
    {
        try
        {
            var resolved = await mailbox.ResolveAuthorsToContactsAsync(new ResolveAuthorsToContactsRequest
            {
                UserId = viewerUserId,
                Provider = BlueskyEntryMapper.IdentityProviderToken,
                ExternalIds = { did },
            }, cancellationToken: ct);

            return resolved.ContactCids.Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not check the contact identities of {User}", viewerUserId);
            return false;
        }
    }

    private async Task<IReadOnlyList<ContactFeedSource>> GetContactSourcesAsync(
        long cid, string viewerUserId, uint capability, CancellationToken ct)
    {
        FeedSubject? subject;
        try
        {
            var response = await mailbox.ResolveFeedSubjectsAsync(
                new ResolveFeedSubjectsRequest { UserId = viewerUserId, Cids = { cid } }, cancellationToken: ct);

            subject = response.Subjects.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not resolve the feed subject for {Cid}", cid);
            return [];
        }

        if (subject == null)
            return [];

        switch (subject.Kind)
        {
            case FeedSubjectKind.ContactIdentity:
                return [.. subject.Identities.Select(i => new ContactFeedSource(i.Provider, i.ExternalId))];

            case FeedSubjectKind.LiveUser:
                return await SharedSourcesAsync(subject.SubjectUserId, capability, ct);

            default:
                logger.LogInformation("Per-contact feed for CID {Cid}: nothing the viewer may read", cid);
                return [];
        }
    }

    // what a subject shares is not viewer-specific, so this caches like the feed does. the decision
    // that the viewer may ask at all is made upstream and never cached
    private async Task<IReadOnlyList<ContactFeedSource>> SharedSourcesAsync(
        string subjectUserId, uint capability, CancellationToken ct)
    {
        var key = $"shared:{subjectUserId}:{capability}";
        if (cache.TryGetValue<IReadOnlyList<ContactFeedSource>>(key, out var cached) && cached != null)
            return cached;

        var sources = await FetchSharedSourcesAsync(subjectUserId, capability, ct);
        cache.Set(key, sources, SharedConnectionsLifetime);
        return sources;
    }

    private async Task<IReadOnlyList<ContactFeedSource>> FetchSharedSourcesAsync(
        string subjectUserId, uint capability, CancellationToken ct)
    {
        try
        {
            var shared = await connectedServices.GetSharedConnectionsAsync(new SharedConnectionsRequest
            {
                UserIds = { subjectUserId },
                Capabilities = capability,
            }, cancellationToken: ct);

            return [.. shared.Connections.Select(c => new ContactFeedSource(c.Service, c.UserId, c.HasUserName ? c.UserName : null))];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the shared connections of {User}", subjectUserId);
            return [];
        }
    }
}
