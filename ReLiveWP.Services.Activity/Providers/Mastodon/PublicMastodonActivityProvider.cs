using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public class PublicMastodonActivityProvider(MastodonActorResolver resolver,
                                            IHttpClientFactory httpClientFactory,
                                            IMemoryCache cache,
                                            ILogger<PublicMastodonActivityProvider> logger) : PublicActivityProviderBase
{
    private const int PageSize = 40;

    public override string Name => "Mastodon";
    public override string ProviderId => MastodonEntryMapper.ProviderId;
    public override string IdentityProvider => MastodonEntryMapper.IdentityProviderToken;

    public override async IAsyncEnumerable<EntryModel> GetAuthorEntriesAsync(string provider, string externalId, int count)
    {
        if (!string.Equals(provider, IdentityProvider, StringComparison.OrdinalIgnoreCase))
            yield break;

        if (!FediverseRequestGuard.TryParseAcceptableUri(externalId, out var actorUri))
            yield break;

        foreach (var entry in (await GetAuthorPageAsync(actorUri)).Take(count))
            yield return entry;
    }

    public override async Task<ResolvedIdentity?> ResolveIdentityAsync(string handleOrId, CancellationToken ct = default)
    {
        var actorUri = await FindActorUriAsync(handleOrId.Trim(), ct);
        if (actorUri == null)
            return null;

        var account = await resolver.FindAccountAsync(actorUri, ct);
        if (account == null)
            return null;

        return new ResolvedIdentity(
            IdentityProvider,
            actorUri.AbsoluteUri,
            account.AccountAddress,
            MastodonEntryMapper.DescribeDisplayName(account.Account, account.AccountAddress),
            MastodonEntryMapper.AcceptableOrEmpty(account.Account.Avatar));
    }

    public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
    {
        if (!MastodonEntryMapper.TryParseActivityId(provider, activityId, out var instance, out var statusId))
            yield break;

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);
        var contextUrl = new Uri(instance, $"/api/v1/statuses/{statusId}/context");

        var replies = await MastodonTimelines.ReadRepliesAsync(http, contextUrl, instance, statusId, selfActorUri: null, count, resolver, logger);
        foreach (var reply in replies)
            yield return reply;
    }

    public async Task<MastodonStatus?> FetchStatusAsync(Uri instance, string statusId, CancellationToken ct = default)
    {
        if (!FediverseRequestGuard.IsAcceptableUri(instance) || !MastodonEntryMapper.IsInstanceId(statusId))
            return null;

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);
        return await MastodonRequests.GetJsonAsync<MastodonStatus>(http, new Uri(instance, $"/api/v1/statuses/{statusId}"), logger, ct);
    }

    private async Task<Uri?> FindActorUriAsync(string handleOrId, CancellationToken ct)
    {
        if (FediverseRequestGuard.TryParseAcceptableUri(handleOrId, out var actorUri) && actorUri.AbsolutePath != "/")
            return actorUri;

        if (FediverseHandle.TryParse(handleOrId, out var handle) && handle.AccountAddress is { } address)
            return await resolver.FindActorUriAsync(address, ct);

        return null;
    }

    // keyed on the actor alone, never on the viewer: public data, identical for everyone
    private async Task<IReadOnlyList<EntryModel>> GetAuthorPageAsync(Uri actorUri)
    {
        var key = $"social:feed:mastodon:{actorUri.AbsoluteUri}";
        if (cache.TryGetValue<IReadOnlyList<EntryModel>>(key, out var cached) && cached != null && cached.Count != 0)
            return cached;

        var page = await FetchAuthorPageAsync(actorUri);
        cache.Set(key, page, SocialAlbumProviderBase.FeedLifetime);
        return page;
    }

    private async Task<IReadOnlyList<EntryModel>> FetchAuthorPageAsync(Uri actorUri)
    {
        var account = await resolver.FindAccountAsync(actorUri);
        if (account == null)
            return [];

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);
        var statusesUrl = new Uri(account.Instance,
            $"/api/v1/accounts/{account.Account.Id}/statuses?limit={PageSize}&exclude_replies=true&exclude_reblogs=true");

        var statuses = await MastodonRequests.GetJsonAsync<MastodonStatus[]>(http, statusesUrl, logger) ?? [];

        return [.. statuses
            .Where(status => status is { Account: not null } && status.Account.Id == account.Account.Id)
            .Where(status => status.Visibility is "public" or "unlisted")
            .Select(status => MastodonEntryMapper.CreateFeedEntry(status, account.Instance, actorUri, selfActorUri: null))
            .OfType<EntryModel>()];
    }
}
