using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

// An owner reads their own album over their own token, and that view can hold posts the appview
// refuses to serve logged out. A contact reading the same album must never land on it.
public class SocialAlbumCacheTests
{
    private const string Owner = "user-a";
    private const string Viewer = "user-b";
    private const string Did = "did:plc:amyamyamyamyamyamyamy";

    private static readonly Connection Owned = new() { Id = "connection-1", Service = "atproto", UserId = Did };

    private static SocialAlbumContents Contents(string marker) =>
        new([new SocialPhoto($"atproto+{Did}+{marker}", $"{marker}.jpg", null, DateTime.UnixEpoch, 1, 1)], "amyy.me");

    private static BlueskyAlbumProvider NewProvider(IMemoryCache cache) =>
        new(null!, cache, NullLoggerFactory.Instance);

    [Fact]
    public void The_owned_view_and_the_public_view_never_share_a_key()
    {
        var pub = SocialAlbumProviderBase.CacheKey("atproto", Did, null);
        var owned = SocialAlbumProviderBase.CacheKey("atproto", Did, Owned);

        Assert.NotEqual(pub, owned);
    }

    // two accounts linking the same identity still read it through their own connection
    [Fact]
    public void Two_owners_of_one_identity_never_share_a_key()
    {
        var mine = SocialAlbumProviderBase.CacheKey("atproto", Did, Owned);
        var theirs = SocialAlbumProviderBase.CacheKey("atproto", Did,
            new Connection { Id = "connection-2", Service = "atproto", UserId = Did });

        Assert.NotEqual(mine, theirs);
    }

    // the public entry is deliberately viewer-independent, that is what makes it shareable
    [Fact]
    public void The_public_key_does_not_depend_on_who_is_asking()
    {
        Assert.Equal(
            SocialAlbumProviderBase.CacheKey("atproto", Did, null),
            SocialAlbumProviderBase.CacheKey("atproto", Did, null));
    }

    [Fact]
    public async Task A_contact_reading_an_album_never_gets_the_owners_authenticated_view()
    {
        var cache = TestCache.New();
        var provider = NewProvider(cache);

        var ownersView = Contents("owner-only");
        var publicView = Contents("public");

        // the owner writes last on purpose: if the two views shared a key, theirs is what a
        // contact would read back, which is exactly the bug this guards
        cache.Set(SocialAlbumProviderBase.CacheKey(provider.Provider, Did, null), publicView);
        cache.Set(SocialAlbumProviderBase.CacheKey(provider.Provider, Did, Owned), ownersView);

        var read = await provider.GetAlbumAsync(Viewer, Did, connection: null);

        Assert.Same(publicView, read);
        Assert.NotSame(ownersView, read);
    }

    [Fact]
    public async Task An_owner_reading_their_own_album_gets_their_own_view()
    {
        var cache = TestCache.New();
        var provider = NewProvider(cache);

        var ownersView = Contents("owner-only");
        var publicView = Contents("public");

        cache.Set(SocialAlbumProviderBase.CacheKey(provider.Provider, Did, Owned), ownersView);
        cache.Set(SocialAlbumProviderBase.CacheKey(provider.Provider, Did, null), publicView);

        var read = await provider.GetAlbumAsync(Owner, Did, Owned);

        Assert.Same(ownersView, read);
        Assert.NotSame(publicView, read);
    }

    // a handle is public either way, so it may be reused. the photos behind it may not
    [Fact]
    public async Task A_handle_from_the_owned_view_may_still_name_a_contacts_album()
    {
        var cache = TestCache.New();
        var provider = NewProvider(cache);

        cache.Set(SocialAlbumProviderBase.CacheKey(provider.Provider, Did, Owned), Contents("owner-only"));

        Assert.Equal("amyy.me", await provider.GetHandleAsync(Owner, Did, Owned));
    }
}
