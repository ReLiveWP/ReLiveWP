using System.Diagnostics.CodeAnalysis;

namespace ReLiveWP.ServiceDefaults.Media;

public enum MediaSize
{
    Avatar,
    Thumb,
    Full,
}

public static class MediaSizes
{
    public const int AvatarEdge = 96;
    public const int ThumbEdge = 320;
    public const int FullEdge = 1024;

    public static string FormatSize(MediaSize size)
    {
        return size switch
        {
            MediaSize.Avatar => "avatar",
            MediaSize.Thumb => "thumb",
            MediaSize.Full => "full",
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
        };
    }

    public static bool TryParseSize(string? value, [NotNullWhen(true)] out MediaSize? size)
    {
        size = value switch
        {
            "avatar" => MediaSize.Avatar,
            "thumb" => MediaSize.Thumb,
            "full" => MediaSize.Full,
            _ => null,
        };

        return size != null;
    }

    public static int MaxEdgeFor(MediaSize size)
    {
        return size switch
        {
            MediaSize.Avatar => AvatarEdge,
            MediaSize.Thumb => ThumbEdge,
            MediaSize.Full => FullEdge,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
        };
    }

    // 0 is how the files routes ask for the original
    public static MediaSize ChooseSizeFor(int requestedEdge)
    {
        if (requestedEdge <= 0)
            return MediaSize.Full;

        if (requestedEdge <= AvatarEdge)
            return MediaSize.Avatar;

        if (requestedEdge <= ThumbEdge)
            return MediaSize.Thumb;

        return MediaSize.Full;
    }
}
