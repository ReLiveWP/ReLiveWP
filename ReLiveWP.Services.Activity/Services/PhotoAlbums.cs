namespace ReLiveWP.Services.Activity.Services;

public static class PhotoAlbums
{
    public const string CategoryPattern = "wmphotos|mobilephotos|twitterphotos";

    public const string AlbumRoute = "/Users({id})/Files/{album:regex(^(" + CategoryPattern + ")$)}";
    public const string PermissionsRoute = AlbumRoute + "/permissions";

    public const string PhotosCategory = "Photos";
    public const string FolderType = "Folder";

    public const string PrivateSharing = "private";
    public const string PublicSharing = "publicshared";

    private static readonly Dictionary<string, string> CanonicalNames = new()
    {
        ["wmphotos"] = "WMPhotos",
        ["mobilephotos"] = "MobilePhotos",
        ["twitterphotos"] = "TwitterPhotos",
    };

    private static readonly Dictionary<string, string> Categories =
        CanonicalNames.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> All => CanonicalNames.Keys;

    public static string? CanonicalNameFor(string category)
        => CanonicalNames.TryGetValue(category, out var name) ? name : null;

    public static bool TryGetCategory(string folderName, out string category)
        => Categories.TryGetValue(folderName, out category!);
}
