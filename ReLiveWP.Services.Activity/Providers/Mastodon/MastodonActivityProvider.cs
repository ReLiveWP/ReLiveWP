using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public class MastodonActivityProvider : OwnedActivityProviderBase
{
    private const int PageSize = 40;
    private const int MaxPages = 5;

    private static readonly TimeSpan OwnAccountLifetime = TimeSpan.FromMinutes(10);

    private readonly HttpClient proxy;
    private readonly MastodonActorResolver resolver;
    private readonly PublicMastodonActivityProvider publicProvider;
    private readonly IMemoryCache cache;
    private readonly ILogger<MastodonActivityProvider> logger;

    private readonly string connectionId;
    private readonly string selfActorUri;
    private readonly Uri? instance;

    public override string Name => "Mastodon";
    public override string ProviderId => MastodonEntryMapper.ProviderId;
    public override string IdentityProvider => MastodonEntryMapper.IdentityProviderToken;

    public MastodonActivityProvider(Connection connection,
                                    HttpClient proxy,
                                    MastodonActorResolver resolver,
                                    PublicMastodonActivityProvider publicProvider,
                                    IMemoryCache cache,
                                    ILogger<MastodonActivityProvider> logger)
    {
        this.proxy = proxy;
        this.resolver = resolver;
        this.publicProvider = publicProvider;
        this.cache = cache;
        this.logger = logger;

        connectionId = connection.Id;
        selfActorUri = connection.UserId;

        if (connection.HasServiceUrl && ExternalRequestGuard.TryParseAcceptableUri(connection.ServiceUrl, out var serviceUrl))
            instance = ExternalRequestGuard.GetInstanceRoot(serviceUrl);
        else
            logger.LogWarning("Mastodon connection {ConnectionId} has no usable instance url", connectionId);
    }

    public override async IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count)
    {
        if (instance == null || count <= 0)
            yield break;

        var path = await GetTimelinePathAsync(context);
        if (path == null)
            yield break;

        var total = 0;
        string? maxId = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = maxId == null ? path : QueryHelpers.AddQueryString(path, "max_id", maxId);
            var statuses = await MastodonRequests.GetJsonAsync<MastodonStatus[]>(proxy, new Uri(url, UriKind.Relative), logger);
            if (statuses is not { Length: > 0 })
                yield break;

            var entries = await MastodonTimelines.MapFeedAsync(statuses, instance, selfActorUri, resolver);
            foreach (var entry in entries)
            {
                if (context == ActivitiesContext.Media && !entry.AdditionalActivities.OfType<PhotoActivityModel>().Any())
                    continue;

                yield return entry;

                if (++total >= count)
                    yield break;
            }

            maxId = statuses[^1].Id;
            if (!MastodonEntryMapper.IsInstanceId(maxId))
                yield break;
        }
    }

    public override async Task CreatePostAsync(string text)
    {
        if (instance == null)
            return;

        if (!await PostStatusAsync(new MastodonStatusRequest(text)))
            throw new HttpRequestException($"Mastodon refused the post for {connectionId}.");
    }

    public override async Task<bool> CreateReplyAsync(string provider, string activityId, string text)
    {
        if (instance == null || !MastodonEntryMapper.TryParseActivityId(provider, activityId, out var origin, out var statusId))
            return false;

        var parent = await FindLocalStatusAsync(origin, statusId);
        if (parent == null || parent.Visibility == "direct")
            return false;

        var ownAccountId = await GetOwnAccountIdAsync();
        var replyText = parent.Account.Id == ownAccountId ? text : MentionAuthor(text, parent.Account);

        var visibility = parent.Visibility is "private" or "unlisted" ? parent.Visibility : null;
        return await PostStatusAsync(new MastodonStatusRequest(replyText, parent.Id, visibility));
    }

    public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
    {
        if (instance == null || !MastodonEntryMapper.TryParseActivityId(provider, activityId, out var origin, out var statusId))
            yield break;

        if (!IsOwnInstance(origin))
        {
            await foreach (var reply in publicProvider.GetRepliesAsync(provider, activityId, count))
                yield return reply;

            yield break;
        }

        var contextUrl = new Uri($"api/v1/statuses/{statusId}/context", UriKind.Relative);
        var replies = await MastodonTimelines.ReadRepliesAsync(proxy, contextUrl, instance, statusId, selfActorUri, count, resolver, logger);

        foreach (var reply in replies)
            yield return reply;
    }

    private bool IsOwnInstance(Uri origin)
        => instance != null && ExternalRequestGuard.IsOnInstance(origin, instance);

    private async Task<string?> GetTimelinePathAsync(ActivitiesContext context)
    {
        switch (context)
        {
            case ActivitiesContext.My:
                var ownAccountId = await GetOwnAccountIdAsync();
                return ownAccountId == null
                    ? null
                    : $"api/v1/accounts/{ownAccountId}/statuses?limit={PageSize}&exclude_replies=true&exclude_reblogs=true";

            case ActivitiesContext.Contacts:
            case ActivitiesContext.Media:
                return $"api/v1/timelines/home?limit={PageSize}";

            default:
                return null;
        }
    }

    private async Task<string?> GetOwnAccountIdAsync()
    {
        var key = $"mastodon:self:{connectionId}";
        if (cache.TryGetValue<string>(key, out var cached) && cached != null)
            return cached;

        var account = await MastodonRequests.GetJsonAsync<MastodonAccount>(
            proxy, new Uri("api/v1/accounts/verify_credentials", UriKind.Relative), logger);

        if (account == null || !MastodonEntryMapper.IsInstanceId(account.Id))
            return null;

        cache.Set(key, account.Id, OwnAccountLifetime);
        return account.Id;
    }

    // ids are per instance, so a status from anywhere else has to be found again on the user's own server first
    private async Task<MastodonStatus?> FindLocalStatusAsync(Uri origin, string statusId)
    {
        if (IsOwnInstance(origin))
            return await MastodonRequests.GetJsonAsync<MastodonStatus>(
                proxy, new Uri($"api/v1/statuses/{statusId}", UriKind.Relative), logger);

        var remote = await publicProvider.FetchStatusAsync(origin, statusId);
        if (remote?.Uri == null || !ExternalRequestGuard.TryParseAcceptableUri(remote.Uri, out var remoteUri))
            return null;

        var searchUrl = $"api/v2/search?q={Uri.EscapeDataString(remoteUri.AbsoluteUri)}&type=statuses&resolve=true&limit=1";
        var results = await MastodonRequests.GetJsonAsync<MastodonSearchResults>(proxy, new Uri(searchUrl, UriKind.Relative), logger);

        return results?.Statuses?.FirstOrDefault(status =>
            status.Uri != null &&
            Uri.TryCreate(status.Uri, UriKind.Absolute, out var found) &&
            found == remoteUri &&
            MastodonEntryMapper.IsInstanceId(status.Id) &&
            status.Account != null);
    }

    private static string MentionAuthor(string text, MastodonAccount author)
    {
        var acct = string.IsNullOrWhiteSpace(author.Acct) ? author.Username : author.Acct;
        var mention = $"@{acct}";

        return text.Contains(mention, StringComparison.OrdinalIgnoreCase) ? text : $"{mention} {text}";
    }

    private async Task<bool> PostStatusAsync(MastodonStatusRequest status)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("api/v1/statuses", UriKind.Relative))
        {
            Content = JsonContent.Create(status, options: FediverseJson.Options),
        };

        // the default http pipeline retries, and this makes a retried post land once
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        using var response = await proxy.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return true;

        logger.LogWarning("Posting to Mastodon for {ConnectionId} failed with {Status}", connectionId, (int)response.StatusCode);
        return false;
    }
}
