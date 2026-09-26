using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class ImageProfileParserTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(176)]
    [InlineData(800)]
    [InlineData(ImageProfileParser.MaxThumbnailSize)]
    public void ParsesWhatTheClientFormatsForThumbnails(int size)
    {
        var formatted = MediaProfiles.ForThumbnail(size);

        Assert.True(ImageProfileParser.TryParseProfile(formatted, out var profile));
        Assert.Equal(new ThumbnailProfile(size), profile);
    }

    [Fact]
    public void ParsesAnAvatarWithAndWithoutACrop()
    {
        Assert.True(ImageProfileParser.TryParseProfile(MediaProfiles.ForAvatar(null), out var plain));
        Assert.Equal(new AvatarProfile(null), plain);

        var crop = new MediaCrop(-50, -50, 250, 250);
        Assert.True(ImageProfileParser.TryParseProfile(MediaProfiles.ForAvatar(crop), out var cropped));
        Assert.Equal(new AvatarProfile(crop), cropped);
    }

    [Fact]
    public void ParsesTheAvatarThumbnail()
    {
        Assert.True(ImageProfileParser.TryParseProfile(MediaProfiles.ForAvatarThumbnail(), out var profile));
        Assert.IsType<AvatarThumbnailProfile>(profile);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesAContactTileWithACropFromEitherOrigin(bool bottomLeft)
    {
        var crop = new MediaCrop(28, 21, 679, 679);

        Assert.True(ImageProfileParser.TryParseProfile(MediaProfiles.ForContactTile(crop, bottomLeft), out var profile));
        Assert.Equal(new ContactTileProfile(crop, bottomLeft), profile);
    }

    [Fact]
    public void ParsesAContactTileWithNoCrop()
    {
        Assert.True(ImageProfileParser.TryParseProfile(MediaProfiles.ForContactTile(null, true), out var profile));
        Assert.Equal(new ContactTileProfile(null, false), profile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("thumb")]
    [InlineData("thumb:")]
    [InlineData("thumb:0")]
    [InlineData("thumb:-96")]
    [InlineData("thumb:+96")]
    [InlineData("thumb: 96")]
    [InlineData("thumb:96 ")]
    [InlineData("thumb:9.6")]
    [InlineData("thumb:1e3")]
    [InlineData("thumb:0x60")]
    [InlineData("thumb:4097")]
    [InlineData("thumb:99999999999")]
    [InlineData("THUMB:96")]
    [InlineData("Avatar")]
    [InlineData("avatar:")]
    [InlineData("avatar:1,2,3")]
    [InlineData("avatar:1,2,3,4,5")]
    [InlineData("avatar:0,0,0,0")]
    [InlineData("avatar:0,0,-5,10")]
    [InlineData("avatar:a,b,c,d")]
    [InlineData("avatar:0,0,10,10:bottom-left")]
    [InlineData("avatar:0,0,2000000,10")]
    [InlineData("avatar-thumb:1,2,3,4")]
    [InlineData("contact-tile:0,0,10,10:top-left")]
    [InlineData("contact-tile:0,0,10,10:bottom-left:x")]
    [InlineData("contact-tile::bottom-left")]
    [InlineData("contact-tile:0,0,10")]
    [InlineData("banner")]
    public void RejectsJunk(string? value)
    {
        Assert.False(ImageProfileParser.TryParseProfile(value, out var profile));
        Assert.Null(profile);
    }
}
