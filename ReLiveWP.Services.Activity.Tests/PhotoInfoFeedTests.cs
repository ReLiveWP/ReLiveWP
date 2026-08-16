using ReLiveWP.Services.Activity.Models.Atom;
using static ReLiveWP.Services.Activity.Tests.AtomSerialization;

namespace ReLiveWP.Services.Activity.Tests;

// Having named a cover, the device fetches that one photo by resourceId. Its parser only descends
// into <entry>, and only insists on a non-empty resourceId and a live:type it recognises.
public class PhotoInfoFeedTests
{
    private const string PhotoRef = "atproto+did:plc:amyamyamyamyamyamyamy+bafkreiacbosk5hldxy";
    private static readonly int[] Sizes = [800, 176, 96];

    private static LivePhotoInfoFeed Feed()
    {
        var mediaBase = $"http://api.live.test/Users(1)/Files/files('{PhotoRef}')";
        var entry = new LiveAlbumItemEntry
        {
            Id = mediaBase,
            ResourceId = PhotoRef,
            Type = "Photo",
            Title = null,
        };

        foreach (var size in Sizes)
            entry.Thumbnails.Add(new LiveMediaThumbnail { Url = $"{mediaBase}/thumbnail/{size}", MaxWidth = size });

        var feed = new LivePhotoInfoFeed { Id = mediaBase };
        feed.Entries.Add(entry);
        return feed;
    }

    [Fact]
    public void TheFeedCarriesExactlyOneEntry()
    {
        var xml = Serialize(Feed());

        Assert.Equal(1, xml.Split("<a:entry").Length - 1);
    }

    [Fact]
    public void TheEntryIsAPhotoWithAResourceId()
    {
        var xml = Serialize(Feed());

        Assert.Contains("<live:type>Photo</live:type>", xml);
        Assert.Contains($"<live:resourceId>{PhotoRef}</live:resourceId>", xml);
    }

    // 176 is the one the cover is drawn from; the other two back the grid tile and the full view
    [Fact]
    public void TheEntryOffersAllThreeRenditions()
    {
        var xml = Serialize(Feed());

        foreach (var size in Sizes)
            Assert.Contains($"maxWidth=\"{size}\"", xml);
    }

    // absent dimensions read as 0 on the device and pass its sanity gate, so claiming 0x0 is
    // needless where we have not been told the real size
    [Fact]
    public void UnknownDimensionsAreOmitted()
    {
        var xml = Serialize(Feed());

        Assert.DoesNotContain("width=\"0\"", xml);
        Assert.DoesNotContain("height=\"0\"", xml);
    }

    // live:type is what decides whether the photo is kept at all, so nothing at feed level may
    // shadow the entry's own
    [Fact]
    public void TheFeedCarriesNoLiveElementsOfItsOwn()
    {
        var feed = new LivePhotoInfoFeed { Id = "http://api.live.test/Users(1)/Files/files('x')" };

        Assert.DoesNotContain("<live:", Serialize(feed));
    }
}
