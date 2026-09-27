using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;

namespace ReLiveWP.Services.MediaProxy.Utilities;

public static class ImageProfileParser
{
    public const int MaxThumbnailSize = 4096;
    public const int MaxCropCoordinate = 1_000_000;

    public static bool TryParseProfile(string? value, [NotNullWhen(true)] out ImageProfile? profile)
    {
        profile = null;
        if (value == null)
            return false;

        if (value.StartsWith(MediaProfiles.ThumbnailPrefix, StringComparison.Ordinal))
            return TryParseThumbnail(value.AsSpan(MediaProfiles.ThumbnailPrefix.Length), out profile);

        var parts = value.Split(MediaProfiles.ArgumentSeparator);

        return parts[0] switch
        {
            MediaProfiles.AvatarName => TryParseAvatar(parts, out profile),
            MediaProfiles.AvatarThumbnailName when parts.Length == 1 => Accept(new AvatarThumbnailProfile(), out profile),
            MediaProfiles.ContactTileName => TryParseContactTile(parts, out profile),
            _ => false,
        };
    }

    private static bool TryParseThumbnail(ReadOnlySpan<char> sizeText, [NotNullWhen(true)] out ImageProfile? profile)
    {
        profile = null;

        if (!int.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out var size))
            return false;

        if (size is < 1 or > MaxThumbnailSize)
            return false;

        profile = new ThumbnailProfile(size);
        return true;
    }

    private static bool TryParseAvatar(string[] parts, [NotNullWhen(true)] out ImageProfile? profile)
    {
        profile = null;

        if (parts.Length == 1)
            return Accept(new AvatarProfile(null), out profile);

        if (parts.Length != 2 || !TryParseCrop(parts[1], out var crop))
            return false;

        return Accept(new AvatarProfile(crop), out profile);
    }

    private static bool TryParseContactTile(string[] parts, [NotNullWhen(true)] out ImageProfile? profile)
    {
        profile = null;

        if (parts.Length == 1)
            return Accept(new ContactTileProfile(null, false), out profile);

        if (parts.Length > 3 || !TryParseCrop(parts[1], out var crop))
            return false;

        if (parts.Length == 3 && parts[2] != MediaProfiles.BottomLeftOrigin)
            return false;

        return Accept(new ContactTileProfile(crop, parts.Length == 3), out profile);
    }

    private static bool TryParseCrop(string value, out MediaCrop crop)
    {
        crop = default;

        var numbers = value.Split(',');
        if (numbers.Length != 4)
            return false;

        var parsed = new int[4];
        for (var i = 0; i < numbers.Length; i++)
        {
            if (!int.TryParse(numbers[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed[i]))
                return false;

            if (Math.Abs((long)parsed[i]) > MaxCropCoordinate)
                return false;
        }

        if (parsed[2] < 1 || parsed[3] < 1)
            return false;

        crop = new MediaCrop(parsed[0], parsed[1], parsed[2], parsed[3]);
        return true;
    }

    private static bool Accept(ImageProfile accepted, out ImageProfile profile)
    {
        profile = accepted;
        return true;
    }
}
