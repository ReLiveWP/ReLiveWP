using System.Globalization;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Web;

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

        var social = SocialEntry.From(entry, "15fe5d7a6d8d65ff");

        Assert.Equal("AT:3kabc", social.Id);
        Assert.Equal("post", social.Type);
        Assert.Equal("15fe5d7a6d8d65ff", social.Author.Cid);
        Assert.False(social.Author.IsMe);
        Assert.Equal("did:plc:amy", social.Author.ExternalId);
        var photo = Assert.Single(social.Photos);
        Assert.Equal("https://cdn/thumb@jpeg", photo.ThumbnailUrl);
        Assert.Equal("image/jpeg", photo.MimeType);
    }

    [Fact]
    public void The_viewers_own_posts_carry_no_cid()
    {
        var social = SocialEntry.From(Post("3kself", isMe: true), "15fe5d7a6d8d65ff");

        Assert.True(social.Author.IsMe);
        Assert.Null(social.Author.Cid);
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
