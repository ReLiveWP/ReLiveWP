using ReLiveWP.Backend.SkyDrive.Services;

namespace ReLiveWP.Backend.SkyDrive.Tests;

public class WebDavItemIdTests
{
    [Theory]
    [InlineData("Windows phone photos/IMG_0001.jpg")]
    [InlineData("album/sub folder/a b c.jpeg")]
    [InlineData("album/ünïcodé.png")]
    [InlineData("a")]
    public void RoundTrips(string path)
    {
        Assert.True(WebDavItemId.TryDecode(WebDavItemId.Encode(path), out var decoded));
        Assert.Equal(path, decoded);
    }

    // SkyDriveService.SplitReference splits on the first '+', and FilesController puts the whole
    // reference in one route segment, so neither character may ever appear in an encoded id.
    [Theory]
    [InlineData("album/a?b>c~d.jpg")]
    [InlineData("photos/ÿþý.jpg")]
    [InlineData("album/subfolder/deeply/nested/file.jpg")]
    public void NeverEmitsPlusOrSlash(string path)
    {
        var encoded = WebDavItemId.Encode(path);

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
    }

    [Fact]
    public void SurvivesSplitReferenceRoundTrip()
    {
        var encoded = WebDavItemId.Encode("Windows phone photos/IMG_0001.jpg");
        var reference = $"webdav+{encoded}";

        var separator = reference.IndexOf('+');
        Assert.Equal("webdav", reference[..separator]);
        Assert.Equal(encoded, reference[(separator + 1)..]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!not base64!!!")]
    public void RejectsGarbage(string itemId)
    {
        Assert.False(WebDavItemId.TryDecode(itemId, out _));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("album/../../../secret")]
    [InlineData("/absolute/path.jpg")]
    public void RejectsTraversalAndAbsolutePaths(string path)
    {
        Assert.False(WebDavItemId.TryDecode(WebDavItemId.Encode(path), out _));
    }
}
