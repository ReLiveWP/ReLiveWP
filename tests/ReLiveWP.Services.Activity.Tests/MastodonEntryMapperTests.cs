using System.Text.Json;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers.Mastodon;

namespace ReLiveWP.Services.Activity.Tests;

public class MastodonEntryMapperTests
{
    private static readonly Uri Snug = new("https://snug.moe/");
    private static readonly Uri Actor = new(MastodonFixture.SnugActor);

    private static MastodonStatus Parse(string json) => JsonSerializer.Deserialize<MastodonStatus>(json, FediverseJson.Options)!;

    private static string Wam(string? uri = null) =>
        MastodonFixture.Account("8zlkadc6ao", "wamwoowam", uri: uri, fqn: "wamwoowam@snug.moe", displayName: "Wam");

    [Fact]
    public void Activity_ids_round_trip()
    {
        var id = MastodonEntryMapper.ComposeActivityId(Snug, "ak1bcxq3u47viorq");

        Assert.Equal("snug.moe+ak1bcxq3u47viorq", id);
        Assert.True(MastodonEntryMapper.TryParseActivityId("MA", id, out var instance, out var statusId));
        Assert.Equal(Snug, instance);
        Assert.Equal("ak1bcxq3u47viorq", statusId);
    }

    [Theory]
    [InlineData("AT", "snug.moe+abc")]
    [InlineData("MA", "snug.moe")]
    [InlineData("MA", "snug.moe+abc+def")]
    [InlineData("MA", "127.0.0.1+abc")]
    [InlineData("MA", "localhost+abc")]
    [InlineData("MA", "metadata.internal+abc")]
    [InlineData("MA", "snug.moe:8080+abc")]
    [InlineData("MA", "snug.moe/evil+abc")]
    [InlineData("MA", "user@snug.moe+abc")]
    [InlineData("MA", "snug.moe+../../admin")]
    [InlineData("MA", "snug.moe+abc?x=1")]
    [InlineData("MA", "snug.moe+abc%2F..")]
    [InlineData("MA", "snug.moe+")]
    public void Activity_ids_that_could_point_anywhere_odd_are_refused(string provider, string activityId)
    {
        Assert.False(MastodonEntryMapper.TryParseActivityId(provider, activityId, out _, out _));
    }

    [Fact]
    public void A_status_maps_to_a_post()
    {
        var entry = MastodonEntryMapper.CreateFeedEntry(Parse(MastodonFixture.Status("s1", Wam())), Snug, Actor, selfActorUri: null)!;

        Assert.Equal("snug.moe+s1", entry.Id);
        Assert.Equal("MA", entry.ProviderId);
        Assert.Equal("hello", entry.Content);
        Assert.Equal(2, entry.ReplyCount);
        Assert.Equal("https://snug.moe/notes/s1", entry.CanonicalUrl);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero), entry.Published);

        Assert.Equal("mastodon", entry.Author.Provider);
        Assert.Equal(MastodonFixture.SnugActor, entry.Author.Id);
        Assert.Equal("@wamwoowam@snug.moe", entry.Author.ScreenName);
        Assert.Equal("Wam", entry.Author.DisplayName);
        Assert.False(entry.Author.IsMe);
    }

    [Fact]
    public void The_signed_in_users_own_posts_are_marked_as_theirs()
    {
        var entry = MastodonEntryMapper.CreateFeedEntry(Parse(MastodonFixture.Status("s1", Wam())), Snug, Actor, MastodonFixture.SnugActor)!;

        Assert.True(entry.Author.IsMe);
    }

    [Fact]
    public void Boosts_are_skipped()
    {
        var boosted = MastodonFixture.Status("orig", Wam());
        var boost = Parse(MastodonFixture.Status("b1", Wam(), content: "", reblog: boosted));

        Assert.Null(MastodonEntryMapper.CreateFeedEntry(boost, Snug, Actor, null));
        Assert.Null(MastodonEntryMapper.MapStatus(boost, Snug, Actor, null));
    }

    [Fact]
    public void Replies_stay_out_of_feeds_but_map_as_replies()
    {
        var reply = Parse(MastodonFixture.Status("r1", Wam(), inReplyTo: "s1"));

        Assert.Null(MastodonEntryMapper.CreateFeedEntry(reply, Snug, Actor, null));
        Assert.NotNull(MastodonEntryMapper.MapStatus(reply, Snug, Actor, null));
    }

    [Fact]
    public void Direct_messages_never_map()
    {
        var direct = Parse(MastodonFixture.Status("d1", Wam(), visibility: "direct"));

        Assert.Null(MastodonEntryMapper.MapStatus(direct, Snug, Actor, null));
    }

    [Fact]
    public void A_content_warning_leads_the_text()
    {
        var entry = MastodonEntryMapper.MapStatus(Parse(MastodonFixture.Status("s1", Wam(), spoiler: "food")), Snug, Actor, null)!;

        Assert.Equal("CW: food\n\nhello", entry.Content);
    }

    [Fact]
    public void Images_become_photos_and_other_media_does_not()
    {
        const string media = """
            [{"id":"m1","type":"image","url":"https://snug.moe/files/full.png","preview_url":"https://snug.moe/files/thumb.png"},
             {"id":"m2","type":"video","url":"https://snug.moe/files/clip.mp4","preview_url":"https://snug.moe/files/clip.png"}]
            """;

        var entry = MastodonEntryMapper.MapStatus(Parse(MastodonFixture.Status("s1", Wam(), media: media)), Snug, Actor, null)!;
        var photo = Assert.Single(entry.AdditionalActivities.OfType<PhotoActivityModel>());

        Assert.Equal("https://snug.moe/files/full.png", photo.FullSizeUrl);
        Assert.Equal("https://snug.moe/files/thumb.png", photo.ThumbnailUrl);
        Assert.Equal("image/png", photo.MimeType);
        Assert.Contains("photo", entry.Categories);
    }

    [Theory]
    [InlineData("http://snug.moe/files/full.png")]
    [InlineData("https://10.0.0.1/files/full.png")]
    [InlineData("https://metadata.internal/files/full.png")]
    [InlineData("javascript:alert(1)")]
    public void Media_that_isnt_on_a_public_https_host_is_dropped(string url)
    {
        var media = $$"""[{"id":"m1","type":"image","url":"{{url}}","preview_url":"{{url}}"}]""";
        var entry = MastodonEntryMapper.MapStatus(Parse(MastodonFixture.Status("s1", Wam(), media: media)), Snug, Actor, null)!;

        Assert.Empty(entry.AdditionalActivities);
    }

    [Fact]
    public void An_unsafe_avatar_is_blanked()
    {
        var account = MastodonFixture.Account("1", "wam", avatar: "http://10.0.0.1/a.png");
        var entry = MastodonEntryMapper.MapStatus(Parse(MastodonFixture.Status("s1", account)), Snug, Actor, null)!;

        Assert.Equal("", entry.Author.AvatarUrl);
    }

    [Theory]
    [InlineData("wam", null, null, "wam@snug.moe")]
    [InlineData("wam", "wam@example.social", null, "wam@example.social")]
    [InlineData("wam", null, "wam@remote.example", "wam@remote.example")]
    [InlineData("wam", "Gargron@mastodon.social", null, "wam@snug.moe")]
    public void Account_addresses_prefer_what_the_server_says_when_it_is_consistent(string username, string? fqn, string? acct, string expected)
    {
        var account = new MastodonAccount("1", username, acct ?? username, fqn, null, null, null, null);

        Assert.Equal(expected, MastodonEntryMapper.DescribeAccountAddress(account, Snug));
    }
}
