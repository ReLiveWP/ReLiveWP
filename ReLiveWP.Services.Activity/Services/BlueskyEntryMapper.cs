using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Services;

public static class BlueskyEntryMapper
{
    public const string ProviderId = "AT";
    public const string IdentityProviderToken = "atproto";
    public const string NoUnauthenticatedLabel = "!no-unauthenticated";

    public static ATUri? ParseActivityIdToUri(string provider, string activityId)
    {
        if (string.Compare(ProviderId, provider, true) != 0)
            return null;

        // id format: "{identity}+{collection}+{rkey}" => at://{identity}/{collection}/{rkey}
        var parts = activityId.Split('+');
        if (parts.Length != 3)
            return null;

        return new ATUri($"at://{parts[0]}/{parts[1]}/{parts[2]}");
    }

    public static bool OptedOutOfLoggedOutViewing(PostView? postView) =>
        postView?.Author?.Labels?.Any(l => string.Equals(l.Val, NoUnauthenticatedLabel, StringComparison.Ordinal)) ?? false;

    public static EntryModel? CreatePostEntry(FeedViewPost feedViewPost, ATDid? selfDid)
    {
        if (feedViewPost.Post is not { Record: Post post } postView || feedViewPost.Reply is { })
            return null;

        return MapPostView(postView, post, selfDid);
    }

    public static EntryModel MapPostView(PostView postView, Post post, ATDid? selfDid)
    {
        var author = new ProfileModel()
        {
            IsMe = selfDid != null && postView.Author.Did.Equals(selfDid),
            Provider = IdentityProviderToken,
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
            ProviderId = ProviderId,
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

    public static string? FixImageUrl(string? url)
    {
        if (url == null) return url;

        return $"{url}@jpeg";
    }
}
