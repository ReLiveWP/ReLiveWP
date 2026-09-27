using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Services;

public record ContactFeedSource(string Provider, string ExternalId, string? Handle = null);

public enum OwnedProviderUse
{
    Read,
    Post,
}

public class ActivityProviderService(
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<PublicActivityProviderBase> publicProviders,
    IEnumerable<IOwnedActivityProviderFactory> ownedProviderFactories,
    ConnectedServices.ConnectedServicesClient connectedServices,
    MailboxStore.MailboxStoreClient mailbox,
    IMemoryCache cache,
    ILogger<ActivityProviderService> logger)
{
    private static readonly TimeSpan SharedConnectionsLifetime = TimeSpan.FromSeconds(45);

    private const ulong BustedFlag = 0x80000000UL;
    private const uint SocialFeedCapability = 0x20;
    private const uint SocialPostCapability = 0x40;
    private const uint SocialPhotosCapability = 0x1000;

    private readonly Dictionary<string, IOwnedActivityProviderFactory> ownedProviderFactories =
        ownedProviderFactories.ToDictionary(f => f.IdentityProvider, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PublicActivityProviderBase> PublicProviders { get; } = [.. publicProviders];

    public PublicActivityProviderBase? FindPublicProvider(string identityProvider)
        => PublicProviders.FirstOrDefault(p => string.Equals(p.IdentityProvider, identityProvider, StringComparison.OrdinalIgnoreCase));

    public async Task<OwnedActivityProviderBase?> GetOwnedProviderAsync(OwnedProviderUse use)
    {
        var owned = await CreateOwnedProvidersAsync(use);
        return owned == null ? null : new FeedCoalescingActivityProvider(owned, logger);
    }

    // replies are public, so a network the viewer hasn't linked is still read through its public provider
    public async Task<IReadOnlyList<ActivityProviderBase>> GetReplyProvidersAsync()
    {
        var owned = await CreateOwnedProvidersAsync(OwnedProviderUse.Read) ?? [];
        var unlinked = PublicProviders.Where(p => !owned.Any(o =>
            string.Equals(o.IdentityProvider, p.IdentityProvider, StringComparison.OrdinalIgnoreCase)));

        return [new FeedCoalescingActivityProvider(owned, logger), .. unlinked];
    }

    private async Task<IReadOnlyList<OwnedActivityProviderBase>?> CreateOwnedProvidersAsync(OwnedProviderUse use)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        var userId = context.User.Id()!;

        var required = use == OwnedProviderUse.Post ? SocialPostCapability : SocialFeedCapability;
        var servicesResponse = connectedServices.GetConnections(new ConnectionsRequest { Capabilities = required });

        List<OwnedActivityProviderBase> providers = [];
        await foreach (var connection in servicesResponse.ResponseStream.ReadAllAsync())
        {
            if ((connection.Flags & BustedFlag) != 0)
                continue;

            if (ownedProviderFactories.TryGetValue(connection.Service, out var factory))
                providers.Add(factory.Create(userId, connection));
        }

        return providers;
    }

    public Task<IReadOnlyList<ContactFeedSource>> GetContactFeedSourcesAsync(
        long cid, string viewerUserId, CancellationToken ct = default)
        => GetContactSourcesAsync(cid, viewerUserId, SocialFeedCapability, ct);

    public Task<IReadOnlyList<ContactFeedSource>> GetContactPhotoSourcesAsync(
        long cid, string viewerUserId, CancellationToken ct = default)
        => GetContactSourcesAsync(cid, viewerUserId, SocialPhotosCapability, ct);

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

    public async Task<bool> CanServeIdentityAsync(
        string provider, string externalId, long? subjectCid, string viewerUserId, CancellationToken ct = default)
    {
        if (await OwnsIdentityAsync(provider, externalId, ct))
            return true;

        if (await HasContactIdentityAsync(provider, externalId, viewerUserId, ct))
            return true;

        if (subjectCid is not { } cid)
            return false;

        var sources = await GetContactPhotoSourcesAsync(cid, viewerUserId, ct);
        return sources.Any(s => s.Provider == provider
                                && string.Equals(s.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> OwnsIdentityAsync(string provider, string externalId, CancellationToken ct)
    {
        var call = connectedServices.GetConnections(new ConnectionsRequest(), cancellationToken: ct);
        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
        {
            if (connection.Service == provider &&
                string.Equals(connection.UserId, externalId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> HasContactIdentityAsync(string provider, string externalId, string viewerUserId, CancellationToken ct)
    {
        try
        {
            var resolved = await mailbox.ResolveAuthorsToContactsAsync(new ResolveAuthorsToContactsRequest
            {
                UserId = viewerUserId,
                Provider = provider,
                ExternalIds = { externalId },
            }, cancellationToken: ct);

            return resolved.ContactCids.Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not check the contact identities of {User}", viewerUserId);
            return false;
        }
    }

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
