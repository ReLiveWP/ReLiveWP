using System.Globalization;

namespace ReLiveWP.ServiceDefaults.Media;

public readonly record struct MediaCrop(int X, int Y, int Width, int Height);

public static class MediaProfiles
{
    public const string OutputContentType = "image/jpeg";

    public const string ThumbnailPrefix = "thumb:";
    public const string AvatarName = "avatar";
    public const string AvatarThumbnailName = "avatar-thumb";
    public const string ContactTileName = "contact-tile";
    public const string BottomLeftOrigin = "bottom-left";
    public const char ArgumentSeparator = ':';

    public const int ContactTileEdge = 170;

    public static string ForThumbnail(int maxSize)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{ThumbnailPrefix}{maxSize}");
    }

    public static string ForAvatar(MediaCrop? crop)
    {
        if (crop is not { } rect)
            return AvatarName;

        return $"{AvatarName}{ArgumentSeparator}{FormatCrop(rect)}";
    }

    public static string ForAvatarThumbnail()
    {
        return AvatarThumbnailName;
    }

    public static string ForContactTile(MediaCrop? crop, bool originIsBottomLeft)
    {
        if (crop is not { } rect)
            return ContactTileName;

        var profile = $"{ContactTileName}{ArgumentSeparator}{FormatCrop(rect)}";
        return originIsBottomLeft ? $"{profile}{ArgumentSeparator}{BottomLeftOrigin}" : profile;
    }

    private static string FormatCrop(MediaCrop crop)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{crop.X},{crop.Y},{crop.Width},{crop.Height}");
    }
}
