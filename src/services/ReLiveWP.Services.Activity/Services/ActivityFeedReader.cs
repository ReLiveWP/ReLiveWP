using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Services;

public record ResolvedEntry(EntryModel Entry, long? AuthorCid);

public record PinnedContactSources(long Cid, IReadOnlyList<ContactFeedSource> Sources);

public class ActivityFeedReader(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<ActivityFeedReader> logger)
{
    public async Task<List<ResolvedEntry>> ReadOwnFeedAsync(
        OwnedActivityProviderBase provider,
        ActivitiesContext context,
        int count,
        string userId)
    {
        var entries = await provider.GetEntriesAsync(context, count).ToListAsync();
        return await PairWithAuthorCidsAsync(entries, userId);
    }

    public async Task<List<ResolvedEntry>> ReadNotificationsAsync(
        OwnedActivityProviderBase provider,
        int count,
        DateTimeOffset? since,
        string userId)
    {
        var entries = await provider.GetNotificationsAsync(count, since).ToListAsync();
        return await PairWithAuthorCidsAsync(entries, userId);
    }

    public async Task<List<ResolvedEntry>> ReadRepliesAsync(
        IReadOnlyList<ActivityProviderBase> providers,
        string providerId,
        string activityId,
        int count,
        string userId)
    {
        var fetches = providers.Select(provider => provider.GetRepliesAsync(providerId, activityId, count).ToListAsync().AsTask());
        var pages = await Task.WhenAll(fetches);

        var entries = pages.SelectMany(page => page).ToList();
        return await PairWithAuthorCidsAsync(entries, userId);
    }

    public async Task<List<ResolvedEntry>> ReadContactFeedAsync(
        IReadOnlyList<PublicActivityProviderBase> providers,
        IReadOnlyList<ContactFeedSource> sources,
        long cid,
        int count)
    {
        if (sources.Count == 0)
            return [];

        // providers ignore identities they don't own (provider-token mismatch)
        var fetches = sources.SelectMany(source => providers.Select(provider =>
            provider.GetAuthorEntriesAsync(source.Provider, source.ExternalId, count).ToListAsync().AsTask()));
        var pages = await Task.WhenAll(fetches);

        var entries = pages.SelectMany(page => page).ToList();

        logger.LogInformation(
            "Per-contact feed for CID {Cid}: {Sources} source(s), {Entries} entries",
            cid, sources.Count, entries.Count);

        return [.. entries
            .OrderByDescending(e => e.Published)
            .Take(count)
            .Select(entry => new ResolvedEntry(entry, cid))];
    }

    public async Task<List<ResolvedEntry>> ReadPinnedContactsFeedAsync(
        IReadOnlyList<PublicActivityProviderBase> providers,
        IReadOnlyList<PinnedContactSources> contacts,
        int count)
    {
        var fetches = contacts.Select(contact => ReadContactFeedAsync(providers, contact.Sources, contact.Cid, count));
        var feeds = await Task.WhenAll(fetches);

        return [.. feeds
            .SelectMany(feed => feed)
            .OrderByDescending(resolved => resolved.Entry.Published)
            .Take(count)];
    }

    private async Task<List<ResolvedEntry>> PairWithAuthorCidsAsync(List<EntryModel> entries, string userId)
    {
        var cidByAuthor = await ResolveAuthorCidsAsync(entries, userId);
        return [.. entries.Select(entry => new ResolvedEntry(
            entry,
            entry.Author.IsMe ? null : cidByAuthor[(entry.Author.Provider, entry.Author.Id)]))];
    }

    private async Task<Dictionary<(string Provider, string Id), long>> ResolveAuthorCidsAsync(
        IReadOnlyList<EntryModel> entries, string userId)
    {
        var groups = entries
            .Where(e => !e.Author.IsMe)
            .GroupBy(e => e.Author.Provider)
            .Select(group => (Provider: group.Key, ExternalIds: group.Select(e => e.Author.Id).Distinct().ToList()))
            .ToList();

        var resolved = await Task.WhenAll(groups.Select(group => ResolveProviderAuthorsAsync(group.Provider, group.ExternalIds, userId)));

        var cidByAuthor = new Dictionary<(string Provider, string Id), long>();
        foreach (var (group, contactCids) in groups.Zip(resolved))
        {
            foreach (var externalId in group.ExternalIds)
            {
                cidByAuthor[(group.Provider, externalId)] = contactCids.TryGetValue(externalId, out var contactCid)
                    ? contactCid
                    : Cids.SynthesiseAuthorCid(group.Provider, externalId);
            }
        }

        return cidByAuthor;
    }

    private async Task<IDictionary<string, long>> ResolveProviderAuthorsAsync(string provider, List<string> externalIds, string userId)
    {
        var resolved = await mailbox.ResolveAuthorsToContactsAsync(new ResolveAuthorsToContactsRequest
        {
            UserId = userId,
            Provider = provider,
            ExternalIds = { externalIds },
        });

        logger.LogInformation(
            "Resolved {Count} {Provider} authors, {Bound} bound to contacts",
            externalIds.Count, provider, resolved.ContactCids.Count);

        return resolved.ContactCids;
    }
}
