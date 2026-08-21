using ReLiveWP.Dav;

namespace ReLiveWP.Dav.Tests;

public class DavPathTests
{
    private const string Album = "Windows phone photos";

    [Theory]
    [InlineData("/dav/Windows phone photos/sub/nested.jpg", "sub/nested.jpg")]
    [InlineData("/Windows phone photos/top.jpg", "top.jpg")]
    public void ResolvesPathsBelowTheAlbum(string path, string expected)
        => Assert.Equal(expected, DavPath.RelativeTo(path, Album));

    [Theory]
    [InlineData("/dav/Windows phone photos")]
    [InlineData("/dav/Windows phone photos/")]
    [InlineData("/dav/some other folder/thing.jpg")]
    public void ReturnsNullForTheAlbumItselfAndForOutsiders(string path)
        => Assert.Null(DavPath.RelativeTo(path, Album));

    [Fact]
    public void NormalisesAbsoluteAndRelativeHrefsAlike()
    {
        Assert.Equal("/a/b c.jpg", DavPath.Normalise("http://host/a/b%20c.jpg"));
        Assert.Equal("/a/b c.jpg", DavPath.Normalise("/a/b%20c.jpg"));
        Assert.Equal("/a/b", DavPath.Normalise("/a/b/"));
    }

    // a rooted href is an absolute file: uri on unix, and running it through Uri drops a %25 in
    // front of every escape, so the space never comes back
    [Fact]
    public void RootedHrefsUnescapeWithoutGoingThroughUri()
    {
        Assert.Equal("/photos/Windows phone photos/WP_000084.jpg",
            DavPath.Normalise("/photos/Windows%20phone%20photos/WP_000084.jpg"));

        Assert.Equal("/photos/100% done.jpg", DavPath.Normalise("/photos/100%25%20done.jpg"));
    }

    // hrefs come back as absolute server paths while the proxy resolves under the stored ServiceUrl
    [Theory]
    [InlineData("/addressbooks/wam/personal/", "https://dav.example/", "addressbooks/wam/personal/")]
    [InlineData("/remote.php/dav/addressbooks/wam/contacts/", "https://dav.example/remote.php/dav/", "addressbooks/wam/contacts/")]
    [InlineData("https://dav.example/remote.php/dav/addressbooks/wam/c/", "https://dav.example/remote.php/dav", "addressbooks/wam/c/")]
    [InlineData("/addressbooks/x/", null, "addressbooks/x/")]
    [InlineData("/books/default/card.vcf", "https://dav.example.com/books/", "default/card.vcf")]
    [InlineData("https://dav.example.com/books/default/card.vcf", "https://dav.example.com/books/", "default/card.vcf")]
    [InlineData("/elsewhere/card.vcf", "https://dav.example.com/books/", "elsewhere/card.vcf")]
    public void StripsTheCollectionPrefixFromHrefs(string href, string? root, string expected)
        => Assert.Equal(expected, DavPath.StripPrefix(href, root));

    // the result is a stored item id, so an escape that survives the round trip must stay escaped
    [Fact]
    public void StripPrefixLeavesEscapesAlone()
        => Assert.Equal("default/a%20b.vcf",
            DavPath.StripPrefix("/books/default/a%20b.vcf", "https://dav.example.com/books/"));

    [Fact]
    public void EncodesEachSegmentSeparately()
        => Assert.Equal("Windows%20phone%20photos/IMG%200001.jpg",
            DavPath.Encode("Windows phone photos/IMG 0001.jpg"));
}
