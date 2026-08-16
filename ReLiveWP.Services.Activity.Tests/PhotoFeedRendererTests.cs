using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using static ReLiveWP.Services.Activity.Tests.AtomSerialization;

namespace ReLiveWP.Services.Activity.Tests;

public class PhotoFeedRendererTests
{
    private static readonly FilesUrls Urls = new("http://api.live.test", "1584806899286369791");

    private const string PhotoRef = "webdav+V01dfMDAwMDg0LmpwZw";
    private const string CoverRef = "atproto+did:plc:amyamyamyamyamyamyamy+bafkreiacbosk5hldxy";

    private static Library Album(string coverRef = "") => new()
    {
        Id = "56713d8d-4e02-40d2-8279-718cda188663",
        Type = "Library",
        Category = "wmphotos",
        SharingLevel = "private",
        Title = "Windows phone photos",
        CoverRef = coverRef,
    };

    private static PhotoItem Item(string mediaType = "photo") => new()
    {
        ResourceRef = PhotoRef,
        FileName = "WP_000084.jpg",
        MediaType = mediaType,
        CreatedUnix = 1770000000,
        Width = 3552,
        Height = 2000,
    };

    [Fact]
    public void TheAlbumFeedNamesItsAlbum()
    {
        var feed = PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(), [Item()]);
        var xml = Serialize(feed);

        Assert.Contains("<live:canonicalName>WMPhotos</live:canonicalName>", xml);
        Assert.Contains("<live:itemCount>1</live:itemCount>", xml);
        Assert.Equal("http://api.live.test/Users(1584806899286369791)/Files/wmphotos", feed.Id);
    }

    [Fact]
    public void PhotoEntriesPointAtEveryRendition()
    {
        var xml = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(), [Item()]));

        foreach (var size in (int[])[800, 176, 96])
        {
            Assert.Contains(
                $"url=\"http://api.live.test/Users(1584806899286369791)/Files/files('{PhotoRef}')/thumbnail/{size}\"", xml);
            Assert.Contains($"maxWidth=\"{size}\"", xml);
        }
    }

    // only a video carries media:content, and a photo that grew one would be fetched as a video
    [Fact]
    public void OnlyVideosCarryMediaContent()
    {
        var photo = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(), [Item()]));
        var video = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(), [Item("video")]));

        Assert.DoesNotContain("<media:content", photo);
        Assert.Contains("<live:type>Photo</live:type>", photo);

        Assert.Contains($"<media:content url=\"http://api.live.test/Users(1584806899286369791)/Files/files('{PhotoRef}')/media\"", video);
        Assert.Contains("<live:type>Video</live:type>", video);
    }

    [Fact]
    public void AnAlbumWithNoKnownCoverOmitsTheElement()
    {
        var withCover = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(CoverRef), []));
        var without = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(), []));

        Assert.Contains($"live:resourceId=\"{CoverRef}\"", withCover);
        Assert.DoesNotContain("thumbnail", without);
    }

    [Fact]
    public void TheAlbumFeedWritesItsCoverBeforeTheFirstEntry()
    {
        var xml = Serialize(PhotoFeedRenderer.AlbumFeed(Urls, Urls.Album("wmphotos"), Album(CoverRef), [Item()]));

        Assert.True(xml.IndexOf("thumbnail", StringComparison.Ordinal) < xml.IndexOf("<a:entry", StringComparison.Ordinal));
    }

    [Fact]
    public void LibraryEntriesAddressTheirAlbumByName()
    {
        var entry = PhotoFeedRenderer.LibraryEntry(Urls, Album(CoverRef));

        Assert.Equal("http://api.live.test/Users(1584806899286369791)/Files/wmphotos", entry.Id);
        Assert.Equal("WMPhotos", entry.CanonicalName);
        Assert.Equal(CoverRef, Assert.Single(entry.Thumbnails).ResourceId);
    }

    [Fact]
    public void SocialAlbumEntriesAddressTheirFolderByResourceId()
    {
        var album = new SocialAlbum("atproto+did:plc:amyamyamyamyamyamyamy", "@amyy.me's photos");
        var entry = PhotoFeedRenderer.SocialAlbumEntry(Urls, album, coverRef: null);

        Assert.Equal($"http://api.live.test/Users(1584806899286369791)/Files/folders('{album.ResourceId}')", entry.Id);
        Assert.Equal("publicshared", entry.SharingLevel);
        Assert.Empty(entry.Thumbnails);
    }

    [Fact]
    public void TheSocialAlbumFeedIsDrawnWithItsNewestPhoto()
    {
        var photos = new SocialPhoto[]
        {
            new(CoverRef, "one.jpg", null, DateTime.UtcNow, 0, 0),
            new($"{CoverRef}2", "two.jpg", null, DateTime.UtcNow, 0, 0),
        };

        var feed = PhotoFeedRenderer.SocialAlbumFeed(Urls, "atproto+did:plc:amy", "@amyy.me's photos", photos);

        Assert.Equal(CoverRef, Assert.Single(feed.Thumbnails).ResourceId);
        Assert.Equal(2, feed.ItemCount);
    }

    [Fact]
    public void ThePhotoFeedCarriesOneEntryAndNoAlbumOfItsOwn()
    {
        var xml = Serialize(PhotoFeedRenderer.PhotoFeed(Urls, PhotoRef, title: null));

        Assert.Equal(1, xml.Split("<a:entry").Length - 1);
        Assert.Contains($"<live:resourceId>{PhotoRef}</live:resourceId>", xml);
        Assert.DoesNotContain("width=\"0\"", xml);
    }

    [Fact]
    public void AnUploadedVideoComesBackAsAVideo()
    {
        var entry = PhotoFeedRenderer.UploadedEntry(Urls, new UploadedPhoto(PhotoRef, "clip.mp4", "video"));

        Assert.Equal("Video", entry.Type);
        Assert.Equal($"http://api.live.test/Users(1584806899286369791)/Files/files('{PhotoRef}')", entry.Id);
    }
}
