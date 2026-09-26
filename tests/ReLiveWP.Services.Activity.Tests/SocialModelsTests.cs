using System.Globalization;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Web;
using ReLiveWP.Services.Activity.Providers;

namespace ReLiveWP.Services.Activity.Tests;

public class SocialModelsTests
{
    private const long Cid = 0x15fe5d7a6d8d65ff;

    [Fact]
    public void An_entry_flattens_with_a_composite_id_and_the_given_cid()
    {
        var entry = Post("3kabc", isMe: false);
        entry.AdditionalActivities.Add(new PhotoActivityModel
        {
            Id = "img1",
            CanonicalUrl = "https://bsky.app/p/1",
            ThumbnailUrl = "https://cdn/thumb@jpeg",
            FullSizeUrl = "https://cdn/full@jpeg",
            MimeType = "image/jpeg",
        });

        var social = SocialEntry.From(entry, "15fe5d7a6d8d65ff", TestMediaProxy.Unconfigured);

        Assert.Equal("AT:3kabc", social.Id);
        Assert.Equal("post", social.Type);
        Assert.Equal("15fe5d7a6d8d65ff", social.Author.Cid);
        Assert.False(social.Author.IsMe);
        Assert.Equal("did:plc:amy", social.Author.ExternalId);
        var photo = Assert.Single(social.Photos);
        Assert.Equal("https://cdn/thumb@jpeg", photo.ThumbnailUrl);
    }

    [Fact]
    public void The_viewers_own_posts_carry_no_cid()
    {
        var social = SocialEntry.From(Post("3kself", isMe: true), "15fe5d7a6d8d65ff", TestMediaProxy.Unconfigured);

        Assert.True(social.Author.IsMe);
        Assert.Null(social.Author.Cid);
    }

    [Fact]
    public void Remote_images_go_through_the_media_proxy()
    {
        var entry = Post("3kabc", isMe: false);
        entry.Author.AvatarUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:amy/a@jpeg";
        entry.AdditionalActivities.Add(new PhotoActivityModel
        {
            Id = "img1",
            CanonicalUrl = "https://files.example/statuses/1",
            ThumbnailUrl = "https://files.example/small/1.png",
            FullSizeUrl = "https://files.example/original/1.png",
            MimeType = "image/png",
        });

        var social = SocialEntry.From(entry, "15fe5d7a6d8d65ff", TestMediaProxy.CreateSigner());

        Assert.StartsWith($"{TestMediaProxy.Root}/v1/avatar/", social.Author.AvatarUrl);
        var photo = Assert.Single(social.Photos);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/thumb/", photo.ThumbnailUrl);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/full/", photo.FullSizeUrl);
        Assert.Equal("https://files.example/statuses/1", photo.CanonicalUrl);
    }

    [Fact]
    public void An_image_the_proxy_would_refuse_stays_as_it_was()
    {
        var entry = Post("3kabc", isMe: false);
        entry.AdditionalActivities.Add(new PhotoActivityModel
        {
            Id = "img1",
            CanonicalUrl = "https://files.example/statuses/1",
            ThumbnailUrl = "http://files.example/small/1.png",
            FullSizeUrl = "http://files.example/original/1.png",
            MimeType = "image/png",
        });

        var social = SocialEntry.From(entry, "15fe5d7a6d8d65ff", TestMediaProxy.CreateSigner());

        var photo = Assert.Single(social.Photos);
        Assert.Equal("http://files.example/small/1.png", photo.ThumbnailUrl);
        Assert.Equal("http://files.example/original/1.png", photo.FullSizeUrl);
    }

    [Fact]
    public void A_resolved_identity_avatar_goes_through_the_media_proxy()
    {
        var resolved = new ResolvedIdentity("atproto", "did:plc:amy", "amy.example", "Amy",
                                            "https://cdn.bsky.app/img/avatar/plain/did:plc:amy/a@jpeg");

        var identity = SocialIdentity.From(resolved, TestMediaProxy.CreateSigner());

        Assert.StartsWith($"{TestMediaProxy.Root}/v1/avatar/", identity.AvatarUrl);
    }

    [Fact]
    public void A_missing_avatar_stays_empty()
    {
        var resolved = new ResolvedIdentity("mastodon", "https://snug.moe/users/amy", "amy@snug.moe", "Amy", "");

        var identity = SocialIdentity.From(resolved, TestMediaProxy.CreateSigner());

        Assert.Equal("", identity.AvatarUrl);
    }

    [Fact]
    public void Hex_cids_round_trip_the_way_eas_writes_them()
    {
        var hex = Cid.ToString("x16", CultureInfo.InvariantCulture);

        Assert.Equal("15fe5d7a6d8d65ff", hex);
        Assert.Equal(Cid, long.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static EntryModel Post(string id, bool isMe) => new()
    {
        Id = id,
        ProviderId = "AT",
        EntryType = EntryType.Post,
        Author = new ProfileModel
        {
            IsMe = isMe,
            Provider = "atproto",
            Id = "did:plc:amy",
            DisplayName = "Amy",
            ScreenName = "@amy.example",
            AvatarUrl = "https://cdn/avatar@jpeg",
            CanonicalUrl = "https://bsky.app/profile/amy.example",
        },
        Published = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
        Title = "Post",
        Content = "hello",
        Generator = "Bluesky",
        CanonicalUrl = "https://bsky.app/p/" + id,
        Categories = ["status"],
        ReplyCount = 2,
    };
}
