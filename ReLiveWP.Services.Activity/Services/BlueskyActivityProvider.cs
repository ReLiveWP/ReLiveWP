using System.Diagnostics;
using System.Net;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class BlueskyActivityProvider : ActivityProviderBase
{
    private const string PopularWithFriendsUri = "at://did:plc:z72i7hdynmk6r22z27h6tvur/app.bsky.feed.generator/with-friends";
    private const string TheGramUri = "at://did:plc:vpkhqolt662uhesyj6nxm7ys/app.bsky.feed.generator/followpics";

    private readonly ATProtocol protocol;
    private readonly ATDid did;
    private readonly string handle;

    private readonly IConfiguration configuration;
    private readonly ILogger<BlueskyActivityProvider> logger;

    public override string Name => "Bluesky";
    public override string ProviderId => "AT";

    public BlueskyActivityProvider(string authHeader,
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

        protocol.Client.DefaultRequestHeaders.Add("Authorization", authHeader);
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

                var entry = CreatePostEntry(feedViewPost);
                if (entry == null)
                    continue;

                total++;
                yield return entry;
            }
        } while (total < count && !string.IsNullOrWhiteSpace(cursor));
    }

    public override async Task<bool> CreateReplyAsync(string provider, string activityId, string text)
    {
        var uri = ParseActivityIdToUri(provider, activityId);
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
        var uri = ParseActivityIdToUri(provider, activityId);
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
                yield return MapPostView(postView, post);
                total++;
            }
        }
    }

    private static ATUri? ParseActivityIdToUri(string provider, string activityId)
    {
        if (string.Compare("AT", provider, true) != 0)
            return null;

        // id format: "{identity}+{collection}+{rkey}" => at://{identity}/{collection}/{rkey}
        var parts = activityId.Split('+');
        if (parts.Length != 3)
            return null;

        return new ATUri($"at://{parts[0]}/{parts[1]}/{parts[2]}");
    }

    private EntryModel? CreatePostEntry(FeedViewPost feedViewPost)
    {
        if (feedViewPost.Post is not { Record: Post post } postView || feedViewPost.Reply is { })
            return null;

        return MapPostView(postView, post);
    }

    private EntryModel MapPostView(PostView postView, Post post)
    {
        var author = new ProfileModel()
        {
            IsMe = postView.Author.Did.Equals(this.did),
            Id = $"{postView.Author.Did}",
            ScreenName = $"@{postView.Author.Handle}",
            DisplayName = string.IsNullOrWhiteSpace(postView.Author.DisplayName) ? $"@{postView.Author.Handle}" : postView.Author.DisplayName,
            CanonicalUrl = $"https://anartia.kelinci.net/{postView.Author.Did}",
            AvatarUrl = FixImageUrl(postView.Author.Avatar!)!
        };

        var postId = postView.Uri.Rkey;
        var postEntry = new EntryModel()
        {
            Id = $"{postView.Uri.Identity}+{postView.Uri.Collection}+{postView.Uri.Rkey}",
            ProviderId = this.ProviderId,
            EntryType = EntryType.Post,
            Title = "Post",
            Content = post.Text ?? "",
            Published = post.CreatedAt ?? DateTime.Now,
            Author = author,
            Categories = ["status"],
            Generator = "Bluesky",
            CanonicalUrl = $"https://anartia.kelinci.net/{postView.Author.Did}/{postId}",
            CanReply = !(postView.Viewer?.ReplyDisabled ?? false),
            ReplyCount = (int)(postView.ReplyCount ?? 0),
        };

        if (postView.Embed is ViewImages viewImages)
        {
            postEntry.Categories.Add("media");
            postEntry.Categories.Add("photo");

            foreach (var image in viewImages.Images)
            {
                postEntry.AdditionalActivities.Add(new PhotoActivityModel()
                {
                    Id = image.Fullsize,
                    ThumbnailUrl = FixImageUrl(image.Thumb)!,
                    FullSizeUrl = FixImageUrl(image.Fullsize)!,
                    CanonicalUrl = FixImageUrl(image.Fullsize)!,
                    MimeType = "image/jpeg"
                });
            }
        }

        // TODO: video

        return postEntry;
    }

    private static string? FixImageUrl(string? url)
    {
        if (url == null) return url;

        return $"{url}@jpeg";
    }
}
