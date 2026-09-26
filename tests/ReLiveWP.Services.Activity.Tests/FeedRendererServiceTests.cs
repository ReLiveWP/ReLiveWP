using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Tests;

public class FeedRendererServiceTests
{
    private const string AvatarUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:amy/a@jpeg";
    private const string ThumbUrl = "https://files.example/small/1.png";
    private const string FullUrl = "https://files.example/original/1.png";

    private sealed class StubUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();
        public string? Action(UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => false;
        public string? Link(string? routeName, object? values) => $"http://api.live.test/{routeName}";
        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }

    private static FeedRendererService CreateRenderer(MediaProxyUrlSigner mediaProxy) => new(null!, mediaProxy);

    private static Link ReadAvatarLink(LiveEntry entry)
    {
        var author = Assert.IsType<LiveAuthor>(entry.Author);
        return Assert.Single(author.Links);
    }

    private static EntryModel Post(string avatarUrl, string thumbUrl, string fullUrl)
    {
        var entry = new EntryModel
        {
            Id = "3kabc",
            ProviderId = "AT",
            EntryType = EntryType.Post,
            Author = new ProfileModel
            {
                IsMe = false,
                Provider = "atproto",
                Id = "did:plc:amy",
                DisplayName = "Amy",
                ScreenName = "@amy.example",
                AvatarUrl = avatarUrl,
                CanonicalUrl = "https://bsky.app/profile/amy.example",
            },
            Published = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            Title = "Post",
            Content = "hello",
            Generator = "Bluesky",
            CanonicalUrl = "https://bsky.app/p/3kabc",
            Categories = ["status"],
        };

        entry.AdditionalActivities.Add(new PhotoActivityModel
        {
            Id = "img1",
            CanonicalUrl = "https://files.example/statuses/1",
            ThumbnailUrl = thumbUrl,
            FullSizeUrl = fullUrl,
            MimeType = "image/png",
        });

        return entry;
    }

    [Fact]
    public void AvatarsAndPhotosPointAtTheMediaProxy()
    {
        var renderer = CreateRenderer(TestMediaProxy.CreateSigner());

        var entry = renderer.CreatePostEntry(new StubUrlHelper(), Post(AvatarUrl, ThumbUrl, FullUrl), meAuthor: null, authorCid: 42);

        var avatar = ReadAvatarLink(entry);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/avatar/", avatar.Href);
        Assert.Equal("image/jpeg", avatar.Type);

        var photo = entry.Activities.Single(a => a.ObjectType == "http://activitystrea.ms/schema/1.0/photo");
        var preview = photo.Links.Single(l => l.Relation == "preview");
        var alternate = photo.Links.Single(l => l.Relation == "alternate");
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/thumb/", preview.Href);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/full/", alternate.Href);
        Assert.Equal("image/jpeg", preview.Type);
        Assert.Equal("image/jpeg", alternate.Type);
    }

    [Fact]
    public void WithoutAKeyTheFeedKeepsTheRawUrls()
    {
        var renderer = CreateRenderer(TestMediaProxy.Unconfigured);

        var entry = renderer.CreatePostEntry(new StubUrlHelper(), Post(AvatarUrl, ThumbUrl, FullUrl), meAuthor: null, authorCid: 42);

        Assert.Equal(AvatarUrl, ReadAvatarLink(entry).Href);

        var photo = entry.Activities.Single(a => a.ObjectType == "http://activitystrea.ms/schema/1.0/photo");
        var preview = photo.Links.Single(l => l.Relation == "preview");
        Assert.Equal(ThumbUrl, preview.Href);
        Assert.Equal("image/png", preview.Type);
    }

    [Fact]
    public void AnImageTheProxyWouldRefuseIsNeverSigned()
    {
        var renderer = CreateRenderer(TestMediaProxy.CreateSigner());
        const string privateThumb = "https://nas.local/small/1.png";

        var entry = renderer.CreatePostEntry(new StubUrlHelper(), Post("", privateThumb, "http://files.example/1.png"),
                                             meAuthor: null, authorCid: 42);

        Assert.Equal("", ReadAvatarLink(entry).Href);

        var photo = entry.Activities.Single(a => a.ObjectType == "http://activitystrea.ms/schema/1.0/photo");
        Assert.Equal(privateThumb, photo.Links.Single(l => l.Relation == "preview").Href);
        Assert.Equal("http://files.example/1.png", photo.Links.Single(l => l.Relation == "alternate").Href);
    }
}
