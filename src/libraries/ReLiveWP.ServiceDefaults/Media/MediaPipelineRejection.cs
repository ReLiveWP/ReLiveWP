namespace ReLiveWP.ServiceDefaults.Media;

public sealed record MediaPipelineRejection(string Code, string? Detail = null)
{
    public const string Unreadable = "unreadable";
    public const string TooLarge = "too-large";
    public const string TooManyPixels = "too-many-pixels";
    public const string CropOutside = "crop-outside";
    public const string UnknownProfile = "unknown-profile";
}

public sealed record MediaPipelineResult(byte[]? Jpeg, MediaPipelineRejection? Rejection)
{
    public static MediaPipelineResult Unavailable { get; } = new(null, null);

    public static MediaPipelineResult FromJpeg(byte[] jpeg)
    {
        return new MediaPipelineResult(jpeg, null);
    }

    public static MediaPipelineResult FromRejection(string code, string? detail = null)
    {
        return new MediaPipelineResult(null, new MediaPipelineRejection(code, detail));
    }

    public bool IsUnavailable => Jpeg == null && Rejection == null;
}

public sealed class MediaPipelineUnavailableException() : Exception("the media pipeline is unavailable");
