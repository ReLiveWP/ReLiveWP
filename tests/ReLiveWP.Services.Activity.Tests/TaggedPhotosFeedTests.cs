using ReLiveWP.Services.Activity.Models.Atom;
using static ReLiveWP.Services.Activity.Tests.AtomSerialization;

namespace ReLiveWP.Services.Activity.Tests;

// The device asks PhotosOf for every contact it shows. 404 buckets to 0x83CF0101, which is not
// retriable and kills the job outright, so "no tagged photos" has to be a body it can parse.
public class TaggedPhotosFeedTests
{
    [Fact]
    public void AnEmptyFeedCarriesNoEntries()
    {
        var xml = Serialize(new LiveTaggedPhotosFeed());

        Assert.DoesNotContain("<a:entry", xml);
        Assert.DoesNotContain("<entry", xml);
    }

    // live:type is tag 0x49 in this parser's table and gates whether an item is kept, so a stray
    // one at feed level is not the harmless no-op that an unrecognised element would be
    [Fact]
    public void AnEmptyFeedCarriesNoLiveElements()
    {
        var xml = Serialize(new LiveTaggedPhotosFeed());

        Assert.DoesNotContain("<live:", xml);
    }

    [Fact]
    public void TheFeedRootIsAtom()
    {
        var xml = Serialize(new LiveTaggedPhotosFeed());

        Assert.Contains("feed", xml);
        Assert.Contains("http://www.w3.org/2005/Atom", xml);
    }
}
