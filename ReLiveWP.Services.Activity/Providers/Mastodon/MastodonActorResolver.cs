using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public class MastodonActorResolver(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<MastodonActorResolver> logger)
{
    private const int MaxConcurrentLookups = 4;

    private static readonly TimeSpan ActorLifetime = TimeSpan.FromHours(6);
    private static readonly TimeSpan AccountLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MissLifetime = TimeSpan.FromMinutes(5);

    public async Task<Uri?> FindActorUriAsync(string accountAddress, CancellationToken ct = default)
    {
        if (!FediverseHandle.TryParse(accountAddress, out var handle) || handle.AccountAddress is not { } address)
            return null;

        var key = $"mastodon:actor:{address.ToLowerInvariant()}";
        if (cache.TryGetValue<Uri?>(key, out var cached))
            return cached;

        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);
        var actorUri = await FediverseWebFinger.FindActorUriAsync(http, address, ct);

        cache.Set(key, actorUri, actorUri == null ? MissLifetime : ActorLifetime);
        return actorUri;
    }

    // the instance serving the status vouches for the uri it sends, anything without one goes through webfinger
    public async Task<Uri?> ResolveAuthorAsync(MastodonAccount account, Uri servingInstance, CancellationToken ct = default)
    {
        if (account.Uri != null)
            return ExternalRequestGuard.TryParseAcceptableUri(account.Uri, out var claimed) ? claimed : null;

        var address = MastodonEntryMapper.DescribeAccountAddress(account, servingInstance);
        return await FindActorUriAsync(address, ct);
    }

    public async Task<IReadOnlyDictionary<string, Uri>> ResolveAuthorsAsync(
        IEnumerable<MastodonAccount> accounts, Uri servingInstance, CancellationToken ct = default)
    {
        var distinct = accounts
            .Where(account => MastodonEntryMapper.IsInstanceId(account.Id))
            .DistinctBy(account => account.Id)
            .ToList();

        using var throttle = new SemaphoreSlim(MaxConcurrentLookups);
        var lookups = distinct.Select(async account =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                var actorUri = await ResolveAuthorAsync(account, servingInstance, ct);
                return (account.Id, ActorUri: actorUri);
            }
            finally
            {
                throttle.Release();
            }
        });

        var resolved = await Task.WhenAll(lookups);
        return resolved
            .Where(pair => pair.ActorUri != null)
            .ToDictionary(pair => pair.Id, pair => pair.ActorUri!);
    }

    public async Task<MastodonAccountRef?> FindAccountAsync(Uri actorUri, CancellationToken ct = default)
    {
        if (!ExternalRequestGuard.IsAcceptableUri(actorUri))
            return null;

        var key = $"mastodon:account:{actorUri.AbsoluteUri}";
        if (cache.TryGetValue<MastodonAccountRef?>(key, out var cached))
            return cached;

        var account = await FetchAccountAsync(actorUri, ct);

        cache.Set(key, account, account == null ? MissLifetime : AccountLifetime);
        return account;
    }

    private async Task<MastodonAccountRef?> FetchAccountAsync(Uri actorUri, CancellationToken ct)
    {
        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);

        var address = await FediverseWebFinger.FindAccountAddressAsync(http, actorUri, ct);
        if (address == null)
        {
            logger.LogInformation("{ActorUri} has no webfinger account that points back at it", actorUri);
            return null;
        }

        var instance = ExternalRequestGuard.GetInstanceRoot(actorUri);
        var lookupUrl = new Uri(instance, $"/api/v1/accounts/lookup?acct={Uri.EscapeDataString(address)}");

        var account = await MastodonRequests.GetJsonAsync<MastodonAccount>(http, lookupUrl, logger, ct);
        if (account == null || !MastodonEntryMapper.IsInstanceId(account.Id) || string.IsNullOrEmpty(account.Username))
            return null;

        if (account.Uri != null && !(Uri.TryCreate(account.Uri, UriKind.Absolute, out var claimed) && claimed == actorUri))
        {
            logger.LogWarning("{Instance} looked {Address} up as {Claimed}, not {ActorUri}", instance.IdnHost, address, account.Uri, actorUri);
            return null;
        }

        return new MastodonAccountRef(instance, actorUri, address, account);
    }
}
