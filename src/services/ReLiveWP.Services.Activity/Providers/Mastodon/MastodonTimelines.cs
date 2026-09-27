using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public static class MastodonTimelines
{
    public static async Task<List<EntryModel>> MapFeedAsync(
        IEnumerable<MastodonStatus> statuses, Uri instance, string? selfActorUri, MastodonActorResolver resolver, CancellationToken ct = default)
    {
        var kept = statuses
            .Where(status => status is { Account: not null, Reblog: null, InReplyToId: null })
            .ToList();

        return await MapWithResolvedAuthorsAsync(kept, instance, resolver,
            (status, actorUri) => MastodonEntryMapper.CreateFeedEntry(status, instance, actorUri, selfActorUri), ct);
    }

    public static async Task<List<EntryModel>> ReadRepliesAsync(
        HttpClient http, Uri contextUrl, Uri instance, string statusId, string? selfActorUri, int count,
        MastodonActorResolver resolver, ILogger logger, CancellationToken ct = default)
    {
        var context = await MastodonRequests.GetJsonAsync<MastodonContext>(http, contextUrl, logger, ct);

        var direct = (context?.Descendants ?? [])
            .Where(status => status is { Account: not null } && status.InReplyToId == statusId && status.Visibility != "direct")
            .Take(count)
            .ToList();

        return await MapWithResolvedAuthorsAsync(direct, instance, resolver,
            (status, actorUri) => MastodonEntryMapper.MapStatus(status, instance, actorUri, selfActorUri), ct);
    }

    private static async Task<List<EntryModel>> MapWithResolvedAuthorsAsync(
        List<MastodonStatus> statuses, Uri instance, MastodonActorResolver resolver,
        Func<MastodonStatus, Uri, EntryModel?> mapStatus, CancellationToken ct)
    {
        var authors = await resolver.ResolveAuthorsAsync(statuses.Select(status => status.Account), instance, ct);

        return [.. statuses
            .Select(status => authors.TryGetValue(status.Account.Id, out var actorUri) ? mapStatus(status, actorUri) : null)
            .OfType<EntryModel>()];
    }
}
