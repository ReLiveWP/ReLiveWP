using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Providers.Bluesky;
using ReLiveWP.Services.Activity.Providers.Mastodon;

namespace ReLiveWP.Services.Activity.Tests;

// the me-card notifications pivot parses the same atom the what's-new feed uses, so a notification
// has to survive _InferTypeAndQueueActivity on the phone: post verb, an author, a title
public class NotificationEntryTests
{
    private static NotificationSource Source(NotificationKind kind, string? subjectText = null) => new(
        Id: "did:plc:wam+app.bsky.feed.like+abc",
        Kind: kind,
        Published: new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero),
        ActorId: "did:plc:gargron",
        ActorDisplayName: "Gargron",
        ActorScreenName: "@gargron.example",
        ActorAvatarUrl: "https://example.test/avatar.jpg",
        ActorCanonicalUrl: "https://example.test/gargron",
        SubjectText: subjectText,
        SubjectUrl: "https://example.test/post/1");

    [Fact]
    public void A_notification_is_titled_as_a_sentence_about_the_other_person()
    {
        var entry = NotificationEntries.Create("AT", "atproto", "Bluesky", Source(NotificationKind.Reply, "well quite"));

        Assert.Equal("Gargron replied to your post", entry.Title);
        Assert.False(entry.Author.IsMe);
        Assert.Equal(EntryType.Post, entry.EntryType);
    }

    // the pivot row already says who mentioned you, so the post behind the tap is just their post
    [Fact]
    public void The_content_is_the_mentioning_post_itself()
    {
        var entry = NotificationEntries.Create("AT", "atproto", "Bluesky", Source(NotificationKind.Reply, "  well quite  "));

        Assert.Equal("well quite", entry.Content);
    }

    [Fact]
    public void Without_a_post_the_sentence_stands_in()
    {
        var entry = NotificationEntries.Create("AT", "atproto", "Bluesky", Source(NotificationKind.Mention));

        Assert.Equal("Gargron mentioned you", entry.Content);
    }

    [Theory]
    [InlineData(NotificationKind.Mention, true)]
    [InlineData(NotificationKind.Reply, true)]
    [InlineData(NotificationKind.Quote, true)]
    [InlineData(NotificationKind.Like, false)]
    [InlineData(NotificationKind.Repost, false)]
    [InlineData(NotificationKind.Follow, false)]
    [InlineData(NotificationKind.Poll, false)]
    [InlineData(NotificationKind.Edit, false)]
    [InlineData(NotificationKind.Unknown, false)]
    public void Only_mentions_replies_and_quotes_count(NotificationKind kind, bool counts)
        => Assert.Equal(counts, NotificationEntries.IsMention(kind));

    // the pivot is a list, and a like carries no thread to reply into
    [Fact]
    public void Notifications_are_not_replyable()
    {
        var entry = NotificationEntries.Create("AT", "atproto", "Bluesky", Source(NotificationKind.Repost));

        Assert.False(entry.CanReply);
        Assert.Equal(0, entry.ReplyCount);
    }

    [Theory]
    [InlineData("like", NotificationKind.Like)]
    [InlineData("repost", NotificationKind.Repost)]
    [InlineData("follow", NotificationKind.Follow)]
    [InlineData("mention", NotificationKind.Mention)]
    [InlineData("reply", NotificationKind.Reply)]
    [InlineData("quote", NotificationKind.Quote)]
    [InlineData("starterpack-joined", NotificationKind.Unknown)]
    public void Bluesky_reasons_map_to_kinds(string reason, NotificationKind expected)
        => Assert.Equal(expected, BlueskyNotificationMapper.KindForReason(reason));

    [Theory]
    [InlineData("favourite", NotificationKind.Like)]
    [InlineData("reblog", NotificationKind.Repost)]
    [InlineData("follow", NotificationKind.Follow)]
    [InlineData("follow_request", NotificationKind.Follow)]
    [InlineData("mention", NotificationKind.Mention)]
    [InlineData("poll", NotificationKind.Poll)]
    [InlineData("update", NotificationKind.Edit)]
    [InlineData("admin.report", NotificationKind.Unknown)]
    public void Mastodon_types_map_to_kinds(string type, NotificationKind expected)
        => Assert.Equal(expected, MastodonNotificationMapper.KindForType(type));
}
