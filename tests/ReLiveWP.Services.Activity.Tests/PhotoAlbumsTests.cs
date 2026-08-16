using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Tests;

public class PhotoAlbumsTests
{
    // the route constraint has to be a literal for the attribute, so nothing but a test stops it
    // drifting from the table the rest of the code reads
    [Fact]
    public void TheRouteConstraintListsEveryAlbum()
    {
        Assert.Equal([.. PhotoAlbums.All.Order()], [.. PhotoAlbums.CategoryPattern.Split('|').Order()]);
    }

    [Fact]
    public void EveryAlbumHasACanonicalName()
    {
        foreach (var category in PhotoAlbums.All)
            Assert.False(string.IsNullOrEmpty(PhotoAlbums.CanonicalNameFor(category)));
    }

    [Fact]
    public void ACanonicalNameLeadsBackToItsCategory()
    {
        foreach (var category in PhotoAlbums.All)
        {
            Assert.True(PhotoAlbums.TryGetCategory(PhotoAlbums.CanonicalNameFor(category)!, out var found));
            Assert.Equal(category, found);
        }
    }

    // the device asks for folders('WMPhotos'), the listing calls it wmphotos
    [Fact]
    public void FolderNamesAreMatchedWithoutRegardToCase()
    {
        Assert.True(PhotoAlbums.TryGetCategory("wmPHOTOS", out var found));
        Assert.Equal("wmphotos", found);

        Assert.False(PhotoAlbums.TryGetCategory("56713d8d-4e02-40d2-8279-718cda188663", out _));
    }

    [Fact]
    public void AnUnknownCategoryHasNoCanonicalName()
    {
        Assert.Null(PhotoAlbums.CanonicalNameFor("spacesphotos"));
    }
}
