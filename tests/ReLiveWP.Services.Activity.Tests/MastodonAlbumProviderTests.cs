using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Providers.Mastodon;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

public class MastodonAlbumProviderTests
{
    private const string SnugKey = "snug.moe+8zlkadc6ao";
    private const string SnugStatuses =
        "https://snug.moe/api/v1/accounts/8zlkadc6ao/statuses?only_media=true&exclude_reblogs=true&exclude_replies=true&limit=40";

    private const string GargronActor = "https://mastodon.social/users/Gargron";

    private static readonly string SnugAccountJson = MastodonFixture.Account("8zlkadc6ao", "wamwoowam", fqn: "wamwoowam@snug.moe");
    private static readonly string OtherSnugAccountJson = MastodonFixture.Account("9otheraccount", "someoneelse");

    private readonly MastodonFixture fixture = new();
    private readonly IMemoryCache cache = TestCache.New();
    private readonly MastodonAlbumProvider provider;

    public MastodonAlbumProviderTests()
    {
        provider = NewProvider(cache);
    }

    private MastodonAlbumProvider NewProvider(IMemoryCache albumCache)
        => new(fixture.Resolver, fixture.HttpClientFactory, albumCache, NullLogger<MastodonAlbumProvider>.Instance);

    // snug is iceshrimp, so no uri on accounts and the reverse lookup goes through webfinger
    private void ServeSnugReverse()
    {
        fixture.ServeSnugAccount();
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/8zlkadc6ao", SnugAccountJson);
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);
    }

    private void ServeGargron()
    {
        var gargron = MastodonFixture.Account("1", "Gargron", uri: GargronActor, acct: "Gargron");
        fixture.ServeWebFingerByActor(GargronActor, "acct:Gargron@mastodon.social");
        fixture.Server.OnJson(HttpMethod.Get, "https://mastodon.social/api/v1/accounts/lookup?acct=Gargron%40mastodon.social", gargron);
        fixture.Server.OnJson(HttpMethod.Get, "https://mastodon.social/api/v1/accounts/1", gargron);
    }

    private void ServeStatuses(string url, params string[] statuses)
        => fixture.Server.OnJson(HttpMethod.Get, url, $"[{string.Join(",", statuses)}]");

    private static string Photo(string statusId, string attachmentId, string visibility = "public",
                                string? account = null, bool sensitive = false, string? inReplyTo = null)
        => MastodonFixture.Status(statusId, account ?? SnugAccountJson, visibility: visibility, sensitive: sensitive,
                                  inReplyTo: inReplyTo, media: $"[{MastodonFixture.Attachment(attachmentId)}]");

    private IEnumerable<RecordedRequest> StatusFetches()
        => fixture.Server.Requests.Where(r => r.Uri.AbsolutePath.StartsWith("/api/v1/statuses/"));

    // refs

    [Theory]
    [InlineData("mastodon+snug.moe+8zlkadc6ao", true)]
    [InlineData("mastodon+SNUG.moe+8zlkadc6ao", true)]
    [InlineData("mastodon+snug.moe", false)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+100.200", false)]
    [InlineData("mastodon+metadata.internal+1", false)]
    [InlineData("mastodon+10.0.0.1+1", false)]
    [InlineData("mastodon+localhost+1", false)]
    [InlineData("mastodon+snug.moe:8443+1", false)]
    [InlineData("mastodon+https://snug.moe/users/8zlkadc6ao", false)]
    [InlineData("mastodon+snug.moe+../../evil", false)]
    [InlineData("mastodon+snug.moe+a.b", false)]
    public void Album_ids_name_a_public_host_and_one_account(string albumId, bool valid)
    {
        var albums = new SocialAlbumsService([provider]);

        Assert.Equal(valid, albums.TryResolveAlbum(albumId, out _, out _));
    }

    [Theory]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+100.200", true)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao", false)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+100", false)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+100.200.300", false)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+..%2F.x", false)]
    [InlineData("mastodon+snug.moe+8zlkadc6ao+100.a/b", false)]
    [InlineData("mastodon+metadata.internal+1+100.200", false)]
    public void Photo_refs_carry_a_status_and_an_attachment(string resourceRef, bool valid)
    {
        var albums = new SocialAlbumsService([provider]);

        Assert.Equal(valid, albums.TryResolvePhoto(resourceRef, out _, out _, out _));
    }

    [Fact]
    public void A_photo_is_named_after_its_attachment()
    {
        Assert.Equal("200.jpg", provider.FileNameFor("100.200"));
    }

    // translation

    [Fact]
    public async Task An_iceshrimp_actor_round_trips_through_its_album_key()
    {
        ServeSnugReverse();

        Assert.Equal(SnugKey, await provider.GetAlbumKeyAsync(MastodonFixture.SnugActor));
        Assert.Equal(MastodonFixture.SnugActor, await provider.FindIdentityAsync(SnugKey));
    }

    [Fact]
    public async Task A_mastodon_actor_round_trips_through_its_album_key()
    {
        ServeGargron();

        Assert.Equal("mastodon.social+1", await provider.GetAlbumKeyAsync(GargronActor));
        Assert.Equal(GargronActor, await provider.FindIdentityAsync("mastodon.social+1"));
    }

    [Fact]
    public async Task An_instance_cannot_claim_an_account_that_lives_somewhere_else()
    {
        ServeGargron();
        fixture.Server.OnJson(HttpMethod.Get, "https://evil.example/api/v1/accounts/123",
            MastodonFixture.Account("123", "Gargron", uri: GargronActor));

        Assert.Null(await provider.FindIdentityAsync("evil.example+123"));
    }

    [Fact]
    public async Task An_instance_cannot_claim_another_local_account_either()
    {
        ServeSnugReverse();
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/9otheraccount",
            MastodonFixture.Account("9otheraccount", "wamwoowam", fqn: "wamwoowam@snug.moe"));

        Assert.Null(await provider.FindIdentityAsync("snug.moe+9otheraccount"));
    }

    [Fact]
    public async Task A_reverse_lookup_whose_webfinger_does_not_point_back_is_refused()
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/8zlkadc6ao", SnugAccountJson);
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", "https://snug.moe/users/someoneelse");

        Assert.Null(await provider.FindIdentityAsync(SnugKey));
    }

    [Fact]
    public async Task An_unacceptable_identity_has_no_album_key()
    {
        Assert.Null(await provider.GetAlbumKeyAsync("http://10.0.0.1/users/evil"));
        Assert.Null(await provider.GetAlbumKeyAsync("did:plc:amyamyamyamyamyamyamy"));
        Assert.Empty(fixture.Server.Requests);
    }

    // listing

    [Fact]
    public async Task Only_the_accounts_own_public_images_make_the_album()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses,
            Photo("110", "a110"),
            Photo("109", "a109", visibility: "unlisted"),
            Photo("108", "a108", visibility: "private"),
            Photo("107", "a107", visibility: "direct"),
            Photo("106", "a106", sensitive: true),
            Photo("105", "a105", inReplyTo: "99"),
            Photo("104", "a104", account: OtherSnugAccountJson),
            MastodonFixture.Status("103", SnugAccountJson, reblog: Photo("50", "a50")),
            MastodonFixture.Status("102", SnugAccountJson, media: $"[{MastodonFixture.Attachment("a102", type: "video")}]"),
            MastodonFixture.Status("101", SnugAccountJson, media: $"[{MastodonFixture.Attachment("a101", url: "http://10.0.0.1/a.png")}]"));
        ServeStatuses($"{SnugStatuses}&max_id=101");

        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        Assert.Equal(
            [$"mastodon+{SnugKey}+110.a110", $"mastodon+{SnugKey}+109.a109"],
            contents.Photos.Select(p => p.ResourceRef));
        Assert.Equal("wamwoowam@snug.moe", contents.Handle);
    }

    [Fact]
    public async Task A_photo_carries_its_alt_text_size_and_order()
    {
        ServeSnugReverse();
        var media = $"[{MastodonFixture.Attachment("a1", description: "a cat", width: 1200, height: 900)},{MastodonFixture.Attachment("a2")}]";
        ServeStatuses(SnugStatuses, MastodonFixture.Status("100", SnugAccountJson, media: media));
        ServeStatuses($"{SnugStatuses}&max_id=100");

        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        Assert.Equal(2, contents.Photos.Count);
        var first = contents.Photos[0];
        Assert.Equal("a1.jpg", first.FileName);
        Assert.Equal("a cat", first.Summary);
        Assert.Equal((1200, 900), (first.Width, first.Height));
        Assert.Equal((0, 0), (contents.Photos[1].Width, contents.Photos[1].Height));
        Assert.True(first.Created > contents.Photos[1].Created);
    }

    [Fact]
    public async Task Pages_follow_max_id_until_one_comes_back_empty()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("300", "a300"));
        ServeStatuses($"{SnugStatuses}&max_id=300", Photo("200", "a200"));
        ServeStatuses($"{SnugStatuses}&max_id=200");

        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        Assert.Equal(2, contents.Photos.Count);
        Assert.Equal(3, fixture.Server.Requests.Count(r => r.Uri.AbsolutePath.EndsWith("/statuses")));
    }

    [Fact]
    public async Task Paging_stops_after_five_pages()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("1000", "a1000"));
        for (var id = 1000; id > 900; id--)
            ServeStatuses($"{SnugStatuses}&max_id={id}", Photo($"{id - 1}", $"a{id - 1}"));

        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        Assert.Equal(5, contents.Photos.Count);
        Assert.Equal(5, fixture.Server.Requests.Count(r => r.Uri.AbsolutePath.EndsWith("/statuses")));
    }

    [Fact]
    public async Task An_album_stops_at_150_photos()
    {
        ServeSnugReverse();
        string Page(int top) => string.Join(",", Enumerable.Range(0, 40).Select(i => Photo($"{top - i}", $"a{top - i}")));

        fixture.Server.OnJson(HttpMethod.Get, SnugStatuses, $"[{Page(10000)}]");
        fixture.Server.OnJson(HttpMethod.Get, $"{SnugStatuses}&max_id=9961", $"[{Page(9960)}]");
        fixture.Server.OnJson(HttpMethod.Get, $"{SnugStatuses}&max_id=9921", $"[{Page(9920)}]");
        fixture.Server.OnJson(HttpMethod.Get, $"{SnugStatuses}&max_id=9881", $"[{Page(9880)}]");

        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        Assert.Equal(150, contents.Photos.Count);
        Assert.Equal(4, fixture.Server.Requests.Count(r => r.Uri.AbsolutePath.EndsWith("/statuses")));
    }

    // the album is public-only, so the owner's view and a contact's view are one and the same
    [Fact]
    public async Task The_owner_and_a_contact_get_the_same_album_from_one_fetch()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("110", "a110"), Photo("108", "a108", visibility: "private"));
        ServeStatuses($"{SnugStatuses}&max_id=108");
        var connection = new Connection { Id = "connection-1", Service = "mastodon", UserId = MastodonFixture.SnugActor };

        var owners = await provider.GetAlbumAsync("user-a", SnugKey, connection);
        var contacts = await provider.GetAlbumAsync("user-b", SnugKey, connection: null);

        Assert.Same(owners, contacts);
        Assert.Equal([$"mastodon+{SnugKey}+110.a110"], owners.Photos.Select(p => p.ResourceRef));
        Assert.All(fixture.Server.Requests, r => Assert.False(r.Headers.ContainsKey("Authorization")));
        Assert.Equal(2, fixture.Server.Requests.Count(r => r.Uri.AbsolutePath.EndsWith("/statuses")));
    }

    [Fact]
    public async Task Your_own_album_is_listed_under_its_album_key()
    {
        ServeSnugReverse();
        var connection = new Connection { Id = "connection-1", Service = "mastodon", UserId = MastodonFixture.SnugActor };

        var albums = await provider.GetAlbumsAsync("user-a", [connection]);

        var album = Assert.Single(albums);
        Assert.Equal($"mastodon+{SnugKey}", album.ResourceId);
        Assert.Equal("@wamwoowam@snug.moe's photos", album.Title);
    }

    // media

    [Fact]
    public async Task A_listed_photo_resolves_without_another_fetch()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("110", "a110"));
        ServeStatuses($"{SnugStatuses}&max_id=110");
        await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        var full = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Full);
        var thumb = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Thumb);
        var avatar = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Avatar);

        Assert.Equal("https://media.snug.moe/a110.png", full?.AbsoluteUri);
        Assert.Equal("https://media.snug.moe/a110-small.webp", thumb?.AbsoluteUri);
        Assert.Equal(thumb, avatar);
        Assert.Empty(StatusFetches());
    }

    [Fact]
    public async Task A_cold_photo_is_looked_up_on_its_own_instance()
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110", Photo("110", "a110"));

        var full = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Full);
        var again = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Thumb);

        Assert.Equal("https://media.snug.moe/a110.png", full?.AbsoluteUri);
        Assert.Equal("https://media.snug.moe/a110-small.webp", again?.AbsoluteUri);
        Assert.Single(StatusFetches());
    }

    [Fact]
    public async Task A_missing_preview_falls_back_to_the_full_image()
    {
        var media = $"[{MastodonFixture.Attachment("a110", previewUrl: "http://10.0.0.1/small.png")}]";
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110",
            MastodonFixture.Status("110", SnugAccountJson, media: media));

        var thumb = await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Thumb);

        Assert.Equal("https://media.snug.moe/a110.png", thumb?.AbsoluteUri);
    }

    [Fact]
    public async Task A_ref_cannot_borrow_another_accounts_status()
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110", Photo("110", "a110", account: OtherSnugAccountJson));

        Assert.Null(await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Full));
    }

    // listing the real owner's album warms the media cache, which must not answer for a ref naming someone else
    [Fact]
    public async Task A_warm_photo_does_not_answer_for_another_account()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("110", "a110"));
        ServeStatuses($"{SnugStatuses}&max_id=110");
        await provider.GetAlbumAsync("user-a", SnugKey, connection: null);
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110", Photo("110", "a110"));

        Assert.Null(await provider.ResolveMediaSourceAsync("snug.moe+9otheraccount", "110.a110", MediaSize.Full));
    }

    [Theory]
    [InlineData("private", false, null)]
    [InlineData("direct", false, null)]
    [InlineData("public", true, null)]
    [InlineData("public", false, "99")]
    public async Task A_photo_that_would_not_be_listed_does_not_resolve(string visibility, bool sensitive, string? inReplyTo)
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110",
            Photo("110", "a110", visibility: visibility, sensitive: sensitive, inReplyTo: inReplyTo));

        Assert.Null(await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Full));
    }

    [Fact]
    public async Task A_missing_attachment_does_not_resolve()
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/110", Photo("110", "a110"));

        Assert.Null(await provider.ResolveMediaSourceAsync(SnugKey, "110.a999", MediaSize.Full));
    }

    [Fact]
    public async Task A_deleted_status_does_not_resolve()
    {
        Assert.Null(await provider.ResolveMediaSourceAsync(SnugKey, "110.a110", MediaSize.Full));
    }

    // web render

    [Fact]
    public async Task The_web_album_signs_both_sizes_through_the_proxy()
    {
        ServeSnugReverse();
        ServeStatuses(SnugStatuses, Photo("110", "a110"));
        ServeStatuses($"{SnugStatuses}&max_id=110");
        var contents = await provider.GetAlbumAsync("user-a", SnugKey, connection: null);

        var renderer = new AlbumRenderService(null!, new SocialAlbumsService([provider]), TestMediaProxy.CreateSigner());
        var response = await renderer.RenderSocialLibraryAsync($"mastodon+{SnugKey}", new SocialAlbumFolder("photos", contents.Photos));

        var photo = Assert.Single(response.Photos);
        Assert.Equal("https://media.snug.moe/a110-small.webp", DecodeProxiedSource(photo.ThumbnailUrl));
        Assert.Equal("https://media.snug.moe/a110.png", DecodeProxiedSource(photo.FullSizeUrl));
        Assert.Empty(StatusFetches());
    }

    private static string DecodeProxiedSource(string url)
    {
        Assert.StartsWith(TestMediaProxy.Root, url);
        var encoded = new Uri(url).AbsolutePath.Split('/')[4].Replace(MediaProxyUrlSigner.FileExtension, "");
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
    }
}
