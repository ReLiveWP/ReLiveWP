using System.Diagnostics;
using System.Net;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class BlueskyActivityProvider : OwnedActivityProviderBase
{
    private const string PopularWithFriendsUri = "at://did:plc:z72i7hdynmk6r22z27h6tvur/app.bsky.feed.generator/with-friends";
    private const string TheGramUri = "at://did:plc:vpkhqolt662uhesyj6nxm7ys/app.bsky.feed.generator/followpics";

    private readonly ATProtocol protocol;
    private readonly ATDid did;
    private readonly string handle;

    private readonly IConfiguration configuration;
    private readonly ILogger<BlueskyActivityProvider> logger;

    public override string Name => "Bluesky";
    public override string ProviderId => BlueskyEntryMapper.ProviderId;

    public const string IdentityProviderToken = BlueskyEntryMapper.IdentityProviderToken;
    public override string IdentityProvider => IdentityProviderToken;

    public BlueskyActivityProvider(string userId,
                                   Connection atprotoConnection,
                                   IConfiguration configuration,
                                   ILoggerFactory loggerFactory)
    {
        Debug.Assert(atprotoConnection.Service == "atproto");

        this.configuration = configuration;
        this.logger = loggerFactory.CreateLogger<BlueskyActivityProvider>();

        this.handle = atprotoConnection.UserName;
        this.did = ATDid.Create(atprotoConnection.UserId)!;
        Debug.Assert(this.did != null);

        var protocol = new ATProtocolBuilder()
             .WithInstanceUrl(new Uri(new Uri(this.configuration["Endpoints:ConnectedServices:Proxy"]!), "/proxy/atproto"))
             .WithLogger(logger)
             .EnableAutoRenewSession(false)
             .WithServiceEndpointUponLogin(false)
             .Build();

        protocol.Client.DefaultRequestHeaders.Add("X-User-Id", userId);
        protocol.Client.DefaultRequestHeaders.Add("X-Connection-Id", atprotoConnection.Id);

        this.protocol = protocol;
    }

    public override async Task CreatePostAsync(string text)
    {
        var facets = await ExtractFacetsAsync(text);
        var record = new Post
        {
            Text = text,
            CreatedAt = DateTime.UtcNow,
            Facets = facets
        };

        _ = (await protocol.CreateRecordAsync(did, "app.bsky.feed.post", record))
            .HandleResult();
    }

    public override async IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count)
    {
        var cursor = "";
        var total = 0;
        do
        {
            IList<FeedViewPost> feedViewPosts;

            var toFetch = Math.Clamp(count - total, 10, 100);
            switch (context)
            {
                case ActivitiesContext.My:
                    {
                        var atFeed = (await protocol.Feed.GetAuthorFeedAsync(did, limit: toFetch, cursor: cursor, includePins: false))
                            .HandleResult()!;
                        cursor = WebUtility.UrlEncode(atFeed.Cursor);
                        feedViewPosts = atFeed.Feed;
                        break;
                    }
                case ActivitiesContext.Contacts:
                    {
                        // TODO: this will be configurable
                        var atFeed = (await protocol.GetFeedAsync(new ATUri(PopularWithFriendsUri), limit: toFetch, cursor: cursor))
                            .HandleResult()!;
                        cursor = WebUtility.UrlEncode(atFeed.Cursor);
                        feedViewPosts = atFeed.Feed;
                        break;
                    }
                case ActivitiesContext.Media:
                    {
                        // TODO: this will be configurable
                        var atFeed = (await protocol.GetFeedAsync(new ATUri(TheGramUri), limit: toFetch, cursor: cursor))
                            .HandleResult()!;
                        cursor = WebUtility.UrlEncode(atFeed.Cursor);
                        feedViewPosts = atFeed.Feed;
                        break;
                    }
                default:
                    yield break;
            }


            foreach (var feedViewPost in feedViewPosts)
            {
                if (total == count)
                    break;

                var entry = BlueskyEntryMapper.CreatePostEntry(feedViewPost, did);
                if (entry == null)
                    continue;

                total++;
                yield return entry;
            }
        } while (total < count && !string.IsNullOrWhiteSpace(cursor));
    }

    public override async Task<bool> CreateReplyAsync(string provider, string activityId, string text)
    {
        var uri = BlueskyEntryMapper.ParseActivityIdToUri(provider, activityId);
        if (uri == null)
            return false;

        var replyPostViews = (await protocol.GetPostsAsync([uri]))
            .HandleResult();
        var replyPostView = replyPostViews?.Posts.FirstOrDefault();
        if (replyPostView is not { PostRecord: { } postRecord })
            return false;

        var replyPostReplyDef = postRecord.Reply;
        var replyRef = new ReplyRefDef()
        {
            Root = replyPostReplyDef?.Root ?? new StrongRef() { Uri = replyPostView.Uri, Cid = replyPostView.Cid },
            Parent = new StrongRef() { Uri = replyPostView.Uri, Cid = replyPostView.Cid }
        };

        var facets = await ExtractFacetsAsync(text);

        var record = new Post
        {
            Text = text,
            Reply = replyRef,
            Facets = facets,
            CreatedAt = DateTime.UtcNow
        };

        _ = (await protocol.CreateRecordAsync(did, "app.bsky.feed.post", record))
            .HandleResult();

        return true;
    }

    private async Task<List<Facet>> ExtractFacetsAsync(string text)
    {
        Facet[] handleFacets = [];
        var postHandles = ATHandle.FromPostText(text);
        if (postHandles.Length > 0)
        {
            var feedProfiles = (await protocol.Actor.GetProfilesAsync([.. postHandles]))
                .HandleResult();
            handleFacets = Facet.ForMentions(text, [.. feedProfiles!.Profiles!]);
        }

        var hashtagFacets = Facet.ForHashtags(text);
        var uriFacets = Facet.ForUris(text);
        return [.. handleFacets, .. hashtagFacets, .. uriFacets];
    }

    public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
    {
        var uri = BlueskyEntryMapper.ParseActivityIdToUri(provider, activityId);
        if (uri == null)
            yield break;

        var thread = (await protocol.Feed.GetPostThreadAsync(uri, depth: 1, parentHeight: 0))
            .HandleResult();

        if (thread?.Thread is not ThreadViewPost root || root.Replies == null)
            yield break;

        var total = 0;
        foreach (var reply in root.Replies)
        {
            if (total >= count)
                break;

            if (reply is ThreadViewPost { Post: { Record: Post post } postView })
            {
                yield return BlueskyEntryMapper.MapPostView(postView, post, did);
                total++;
            }
        }
    }
}
