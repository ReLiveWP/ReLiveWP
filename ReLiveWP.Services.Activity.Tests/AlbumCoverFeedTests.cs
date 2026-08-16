using ReLiveWP.Services.Activity.Models.Atom;
using static ReLiveWP.Services.Activity.Tests.AtomSerialization;

namespace ReLiveWP.Services.Activity.Tests;

// An album tile is drawn from a photo the album names, not from anything in the album's own feed.
// The parser that reads the listing takes the resourceId attribute off media:thumbnail and nothing
// else, so these tests are about the shape of that one element.
public class AlbumCoverFeedTests
{
    private const string CoverRef = "atproto+did:plc:amyamyamyamyamyamyamy+bafkreiacbosk5hldxy";

    private static LiveLibraryFeed ListingWith(params LiveMediaThumbnail[] thumbnails)
    {
        var entry = new LiveLibraryEntry
        {
            Id = "http://api.live.test/Users(1)/Files/folders('album')",
            ResourceId = "album",
            Type = "Folder",
            Category = "Photos",
            SharingLevel = "publicshared",
            Title = "@amyy.me's photos",
        };

        entry.Thumbnails.AddRange(thumbnails);

        var feed = new LiveLibraryFeed { Id = "http://api.live.test/Users(1)/Files" };
        feed.Entries.Add(entry);
        return feed;
    }

    [Fact]
    public void ACoverPointerCarriesTheResourceId()
    {
        var xml = Serialize(ListingWith(new LiveMediaThumbnail { ResourceId = CoverRef }));

        // the album feed's parser checks the attribute's namespace, so the prefix is not cosmetic
        Assert.Contains($"live:resourceId=\"{CoverRef}\"", xml);
    }

    // the listing parser never reads url or maxWidth off an album's thumbnail, and maxWidth="0"
    // would be a lie about a rendition we do not have
    [Fact]
    public void ACoverPointerCarriesNothingElse()
    {
        var xml = Serialize(ListingWith(new LiveMediaThumbnail { ResourceId = CoverRef }));

        Assert.DoesNotContain("url=", xml);
        Assert.DoesNotContain("maxWidth", xml);
        Assert.DoesNotContain("width=", xml);
        Assert.DoesNotContain("height=", xml);
    }

    // an empty media:thumbnail deletes the cover the device already has, so an album we know no
    // cover for has to omit the element outright
    [Fact]
    public void AnAlbumWithNoKnownCoverOmitsTheElement()
    {
        var xml = Serialize(ListingWith());

        Assert.DoesNotContain("thumbnail", xml);
    }

    [Fact]
    public void TheListingDeclaresTheMediaPrefix()
    {
        var xml = Serialize(ListingWith(new LiveMediaThumbnail { ResourceId = CoverRef }));

        Assert.Contains("xmlns:media=\"http://search.yahoo.com/mrss/\"", xml);
        Assert.Contains("<media:thumbnail", xml);
    }

    // the album feed's parser latches on the first entry and hard-errors on any album-level element
    // after it, so a feed-level cover has to be written before the entries
    [Fact]
    public void TheAlbumFeedWritesItsCoverBeforeTheFirstEntry()
    {
        var feed = new LiveAlbumFeed { Id = "http://api.live.test/Users(1)/Files/folders('album')", ResourceId = "album" };
        feed.Thumbnails.Add(new LiveMediaThumbnail { ResourceId = CoverRef });
        feed.Entries.Add(new LiveAlbumItemEntry { Id = "photo", ResourceId = CoverRef, Type = "Photo" });

        var xml = Serialize(feed);

        Assert.Contains($"live:resourceId=\"{CoverRef}\"", xml);
        Assert.True(xml.IndexOf("thumbnail", StringComparison.Ordinal) < xml.IndexOf("<a:entry", StringComparison.Ordinal));
    }
}
