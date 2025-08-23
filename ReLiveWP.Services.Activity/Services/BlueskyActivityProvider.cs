using System.Diagnostics;
using System.Net;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public enum ActivitiesContext
{
    My, Contacts, Media
}

public class BlueskyActivityProvider : ActivityProviderBase
{
    private const string PopularWithFriendsUri = "at://did:plc:z72i7hdynmk6r22z27h6tvur/app.bsky.feed.generator/with-friends";
    private const string TheGramUri = "at://did:plc:vpkhqolt662uhesyj6nxm7ys/app.bsky.feed.generator/followpics";

    private readonly ATProtocol protocol;
    private readonly ATDid did;

    private readonly IConfiguration configuration;
    private readonly ILogger<BlueskyActivityProvider> logger;

    public override string Name => "Bluesky";
    public override string ProviderId => "AT";

    public BlueskyActivityProvider(string authHeader,
                                   Connection atprotoConnection,
                                   IConfiguration configuration,
                                   ILogger<BlueskyActivityProvider> logger)
    {
        Debug.Assert(atprotoConnection.Service == "atproto");

        this.configuration = configuration;
        this.logger = logger;

        this.did = ATDid.Create(atprotoConnection.UserId)!;
        Debug.Assert(this.did != null);

        var protocol = new ATProtocolBuilder()
             .WithInstanceUrl(new Uri(this.configuration["Endpoints:ATProtoProxy"]!))
             .WithLogger(logger)
             .EnableAutoRenewSession(false)
             .WithServiceEndpointUponLogin(false)
             .Build();

        protocol.Client.DefaultRequestHeaders.Add("Authorization", authHeader);
        protocol.Client.DefaultRequestHeaders.Add("X-Connection-Id", atprotoConnection.Id);

        this.protocol = protocol;
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

    private EntryModel? CreatePostEntry(FeedViewPost feedViewPost)
    {
        if (feedViewPost.Post is not { Record: Post post } postView || feedViewPost.Reply is { })
            return null;

        var author = new ProfileModel()
        {
            IsMe = postView.Author.Did.Equals(this.did),
            Id = $"at:{postView.Author.Did}",
            DisplayName = string.IsNullOrWhiteSpace(postView.Author.DisplayName) ? $"@{postView.Author.Handle}" : postView.Author.DisplayName,
            CanonicalUrl = $"https://anartia.kelinci.net/{postView.Author.Did}",
            AvatarUrl = postView.Author.Avatar!
        };

        var postId = postView.Uri.Rkey;
        var postEntry = new EntryModel()
        {
            Id = postView.Uri.ToString(),
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
                    ThumbnailUrl = image.Thumb,
                    FullSizeUrl = image.Fullsize,
                    CanonicalUrl = image.Fullsize,
                    MimeType = image.Type
                });
            }
        }

        // TODO: video

        return postEntry;
    }
}
