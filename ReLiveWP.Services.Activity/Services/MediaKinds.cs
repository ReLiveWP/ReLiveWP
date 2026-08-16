namespace ReLiveWP.Services.Activity.Services;

public static class MediaKinds
{
    public const string Photo = "photo";
    public const string Video = "video";

    public const string PhotoEntry = "Photo";
    public const string VideoEntry = "Video";

    public static bool IsVideo(string? mediaType)
        => string.Equals(mediaType, Video, StringComparison.OrdinalIgnoreCase);

    public static string EntryType(string? mediaType) => IsVideo(mediaType) ? VideoEntry : PhotoEntry;
}
