using System.Net;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Services;

public class PublicBlueskyActivityProvider : PublicActivityProviderBase
{
    private const string AppViewUrl = "https://public.api.bsky.app";
    private const int CachedPageSize = 50;

    private readonly ATProtocol protocol;
    private readonly IMemoryCache cache;
    private readonly ILogger<PublicBlueskyActivityProvider> logger;

    internal ATProtocol Protocol => protocol;

    public override string Name => "Bluesky";
    public override string ProviderId => BlueskyEntryMapper.ProviderId;
    public override string IdentityProvider => BlueskyEntryMapper.IdentityProviderToken;

    public PublicBlueskyActivityProvider(ILoggerFactory loggerFactory, IMemoryCache cache)
    {
        this.cache = cache;
        logger = loggerFactory.CreateLogger<PublicBlueskyActivityProvider>();

        protocol = new ATProtocolBuilder()
            .WithInstanceUrl(new Uri(AppViewUrl))
            .WithLogger(logger)
            .EnableAutoRenewSession(false)
            .WithServiceEndpointUponLogin(false)
            .Build();
    }

    public override async IAsyncEnumerable<EntryModel> GetAuthorEntriesAsync(string provider, string externalId, int count)
    {
        if (!string.Equals(provider, IdentityProvider, StringComparison.OrdinalIgnoreCase))
            yield break;

        var targetDid = ATDid.Create(externalId);
        if (targetDid == null)
            yield break;

        foreach (var entry in await GetAuthorPageAsync(targetDid))
        {
            if (count-- <= 0)
                yield break;

            yield return entry;
        }
    }

    // keyed on the did alone, never on the viewer: this is public data and identical for everyone.
    // whether the caller may see it was decided before we got here and is not cached with it
    private async Task<IReadOnlyList<EntryModel>> GetAuthorPageAsync(ATDid did)
    {
        var key = $"social:feed:{did}";
        if (cache.TryGetValue<IReadOnlyList<EntryModel>>(key, out var cached) && cached != null && cached.Count != 0)
            return cached;

        var page = await FetchAuthorPageAsync(did);
        cache.Set(key, page, SocialAlbumProvider.FeedLifetime);
        return page;
    }

    private async Task<IReadOnlyList<EntryModel>> FetchAuthorPageAsync(ATDid did)
    {
        var entries = new List<EntryModel>();
        var cursor = "";

        logger.LogWarning("Fetching unauthenticated feed for {Did}", did);

        do
        {
            List<FeedViewPost> posts;
            try
            {
                var atFeed = (await protocol.Feed.GetAuthorFeedAsync(did, limit: CachedPageSize, cursor: cursor, filter: "posts_no_replies", includePins: false))
                    .HandleResult()!;

                cursor = WebUtility.UrlEncode(atFeed.Cursor);
                posts = [.. atFeed.Feed];
            }
            catch (Exception ex)
            {
                // an account can ask not to be served to logged-out viewers, and the appview says no.
                // that is an answer, not a problem to route around
                logger.LogInformation(ex, "no public feed available for {Did}", did);
                return entries;
            }

            foreach (var feedViewPost in posts)
            {
                // the post author might not always be the author (e.g. retweets) so just skip _this_ post not all of them
                if (BlueskyEntryMapper.OptedOutOfLoggedOutViewing(feedViewPost.Post))
                    continue;

                if (entries.Count >= CachedPageSize)
                    return entries;

                var entry = BlueskyEntryMapper.CreatePostEntry(feedViewPost, selfDid: null);
                if (entry != null)
                    entries.Add(entry);
            }
        } while (entries.Count < CachedPageSize && !string.IsNullOrWhiteSpace(cursor));

        return entries;
    }

    public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
    {
        var uri = BlueskyEntryMapper.ParseActivityIdToUri(provider, activityId);
        if (uri == null)
            yield break;

        List<ThreadViewPost> replies;
        try
        {
            var thread = (await protocol.Feed.GetPostThreadAsync(uri, depth: 1, parentHeight: 0))
                .HandleResult();

            if (thread?.Thread is not ThreadViewPost root || root.Replies == null)
                yield break;

            replies = [.. root.Replies.OfType<ThreadViewPost>()];
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "no public thread available for {Uri}", uri);
            yield break;
        }

        var total = 0;
        foreach (var reply in replies)
        {
            if (total >= count)
                break;

            if (reply.Post is not { Record: Post post } postView)
                continue;

            if (BlueskyEntryMapper.OptedOutOfLoggedOutViewing(postView))
                continue;

            yield return BlueskyEntryMapper.MapPostView(postView, post, selfDid: null);
            total++;
        }
    }
}
