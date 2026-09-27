using ReLiveWP.ServiceDefaults.Contacts;
using ReLiveWP.ServiceDefaults.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Services.MediaProxy.Models;

public sealed class ImageProfileRejectedException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public abstract record ImageProfile
{
    private static readonly int[] DefaultQualitySteps = [82];

    public virtual IReadOnlyList<int> QualitySteps => DefaultQualitySteps;

    public virtual int? MaxOutputBytes => null;

    public virtual Size? ChooseDecodeSize(ImageInfo source)
    {
        return null;
    }

    public abstract void ApplyTransform(IImageProcessingContext context);

    protected static void ResizeToSquare(IImageProcessingContext context, int side)
    {
        context.Resize(new ResizeOptions
        {
            Size = new Size(side, side),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
            Sampler = KnownResamplers.Lanczos3,
        });
    }
}

public sealed record ThumbnailProfile(int MaxSize) : ImageProfile
{
    public override Size? ChooseDecodeSize(ImageInfo source)
    {
        if (source.Width <= MaxSize && source.Height <= MaxSize)
            return null;

        return new Size(MaxSize, MaxSize);
    }

    public override void ApplyTransform(IImageProcessingContext context)
    {
        var current = context.GetCurrentSize();
        if (current.Width <= MaxSize && current.Height <= MaxSize)
            return;

        context.Resize(new ResizeOptions
        {
            Size = new Size(MaxSize, MaxSize),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Lanczos3,
        });
    }
}

// the crop is in oriented coordinates, the browser previews with image-orientation: from-image
public sealed record AvatarProfile(MediaCrop? Crop) : ImageProfile
{
    public const int MaxEdge = 512;

    public override void ApplyTransform(IImageProcessingContext context)
    {
        if (Crop is { } crop)
        {
            var current = context.GetCurrentSize();
            var clamped = Rectangle.Intersect(new Rectangle(crop.X, crop.Y, crop.Width, crop.Height),
                                              new Rectangle(0, 0, current.Width, current.Height));

            if (clamped.Width <= 0 || clamped.Height <= 0)
                throw new ImageProfileRejectedException(MediaPipelineRejection.CropOutside);

            context.Crop(clamped);
        }

        var cropped = context.GetCurrentSize();
        var side = Math.Min(MaxEdge, Math.Min(cropped.Width, cropped.Height));
        ResizeToSquare(context, side);
    }
}

public sealed record AvatarThumbnailProfile : ImageProfile
{
    public const int MaxEdge = 128;

    public override void ApplyTransform(IImageProcessingContext context)
    {
        var current = context.GetCurrentSize();
        var side = Math.Min(MaxEdge, Math.Min(current.Width, current.Height));
        ResizeToSquare(context, side);
    }
}

public sealed record ContactTileProfile(MediaCrop? Crop, bool OriginIsBottomLeft) : ImageProfile
{
    private static readonly int[] ContactQualitySteps = [82, 70, 58, 45];

    public override IReadOnlyList<int> QualitySteps => ContactQualitySteps;

    public override int? MaxOutputBytes => ContactContract.MaxPictureBytes;

    public override void ApplyTransform(IImageProcessingContext context)
    {
        var current = context.GetCurrentSize();

        if (Crop is { } crop && FrameCrop(crop, OriginIsBottomLeft, current.Width, current.Height) is { } frame)
            context.Crop(frame);

        ResizeToSquare(context, MediaProfiles.ContactTileEdge);
    }

    internal static Rectangle? FrameCrop(MediaCrop crop, bool originIsBottomLeft, int width, int height)
    {
        var top = originIsBottomLeft ? height - crop.Y - crop.Height : crop.Y;

        var frame = Rectangle.Intersect(
            new Rectangle(crop.X, top, crop.Width, crop.Height),
            new Rectangle(0, 0, width, height));

        return frame.Width > 0 && frame.Height > 0 ? frame : null;
    }
}
