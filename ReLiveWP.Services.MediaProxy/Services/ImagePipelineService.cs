using System.Globalization;
using System.Threading.RateLimiting;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Services.MediaProxy.Services;

public sealed class ImagePipelineService(ILogger<ImagePipelineService> logger) : IDisposable
{
    public const int MaxSourceBytes = 64 * 1024 * 1024;
    public const long MaxSourcePixels = 64_000_000;
    public const int DecodeBudgetMegapixels = 256;
    public const int ReceiveBudgetMegabytes = 1024;

    private const int CopyBufferSize = 81920;
    private const int PixelsPerMegapixel = 1_000_000;
    private const int BytesPerMegabyte = 1024 * 1024;

    private static readonly DecoderOptions FirstFrameOnly = new() { MaxFrames = 1 };

    private readonly ConcurrencyLimiter receiveBudget = CreateBudget(ReceiveBudgetMegabytes);
    private readonly ConcurrencyLimiter decodeBudget = CreateBudget(DecodeBudgetMegapixels);

    public void Dispose()
    {
        receiveBudget.Dispose();
        decodeBudget.Dispose();
    }

    public Task<MediaPipelineResult> ProcessImageAsync(Stream source, ImageProfile profile, CancellationToken ct = default)
    {
        return ProcessImageAsync(source, profile, MaxSourceBytes, null, ct);
    }

    public async Task<MediaPipelineResult> ProcessImageAsync(Stream source, ImageProfile profile, int maxSourceBytes,
                                                             long? declaredBytes, CancellationToken ct = default)
    {
        if (declaredBytes > maxSourceBytes)
        {
            logger.LogWarning("Refusing {Profile}, source declares {Length} bytes, over {Limit}", profile, declaredBytes, maxSourceBytes);
            return MediaPipelineResult.FromRejection(MediaPipelineRejection.TooLarge);
        }

        var timings = new PipelineStageTimings();
        var weight = WeighReceiveInMegabytes(declaredBytes, maxSourceBytes);

        RateLimitLease lease;
        using (timings.MeasureStage("admit"))
            lease = await receiveBudget.AcquireAsync(weight, ct);

        using (lease)
        {
            if (!lease.IsAcquired)
                throw new InvalidOperationException($"Could not acquire {weight} MB of receive budget.");

            using var buffered = new MemoryStream((int)(declaredBytes ?? 0));
            bool received;
            using (timings.MeasureStage("receive"))
                received = await TryBufferSourceAsync(source, buffered, maxSourceBytes, ct);

            if (!received)
            {
                logger.LogWarning("Refusing {Profile}, source exceeds {Limit} bytes", profile, maxSourceBytes);
                return MediaPipelineResult.FromRejection(MediaPipelineRejection.TooLarge);
            }

            buffered.Position = 0;
            return await ProcessBufferedImageAsync(buffered, profile, timings, ct);
        }
    }

    internal static async Task<Image> DecodeFirstFrameAsync(Stream source, Size? decodeSize, CancellationToken ct)
    {
        var options = new DecoderOptions
        {
            MaxFrames = 1,
            TargetSize = decodeSize,
            Sampler = KnownResamplers.Lanczos3,
        };

        return await Image.LoadAsync(options, source, ct);
    }

    internal static int WeighDecodeInMegapixels(ImageInfo info)
    {
        var pixels = (long)info.Width * info.Height;
        var megapixels = (pixels + PixelsPerMegapixel - 1) / PixelsPerMegapixel;
        return (int)Math.Clamp(megapixels, 1, DecodeBudgetMegapixels);
    }

    internal static int WeighReceiveInMegabytes(long? declaredBytes, int maxSourceBytes)
    {
        var expected = Math.Min(declaredBytes ?? maxSourceBytes, maxSourceBytes);
        var megabytes = (expected + BytesPerMegabyte - 1) / BytesPerMegabyte;
        return (int)Math.Clamp(megabytes, 1, ReceiveBudgetMegabytes);
    }

    private static ConcurrencyLimiter CreateBudget(int permits)
    {
        return new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }

    private async Task<MediaPipelineResult> ProcessBufferedImageAsync(MemoryStream buffered, ImageProfile profile,
                                                                      PipelineStageTimings timings, CancellationToken ct)
    {
        ImageInfo? info;
        using (timings.MeasureStage("identify"))
            info = await TryIdentifyImageAsync(buffered, profile, ct);

        if (info == null)
            return MediaPipelineResult.FromRejection(MediaPipelineRejection.Unreadable);

        if ((long)info.Width * info.Height > MaxSourcePixels)
        {
            logger.LogWarning("Refusing {Profile}, {Width}x{Height} is too large", profile, info.Width, info.Height);
            var dimensions = string.Create(CultureInfo.InvariantCulture, $"{info.Width}x{info.Height}");
            return MediaPipelineResult.FromRejection(MediaPipelineRejection.TooManyPixels, dimensions);
        }

        buffered.Position = 0;
        var weight = WeighDecodeInMegapixels(info);
        var queuedAhead = decodeBudget.GetStatistics()?.CurrentQueuedCount ?? 0;

        RateLimitLease lease;
        using (timings.MeasureStage("queue"))
            lease = await decodeBudget.AcquireAsync(weight, ct);

        using (lease)
        {
            if (!lease.IsAcquired)
                throw new InvalidOperationException($"Could not acquire {weight} MP of decode budget.");

            var decodeSize = profile.ChooseDecodeSize(info);
            var result = await TranscodeImageAsync(buffered, profile, decodeSize, timings, ct);

            if (result.Jpeg != null)
                logger.LogInformation(
                    "Processed {Profile} from {Width}x{Height}, {SourceBytes} B to {OutputBytes} B, {QueuedAhead} queued ahead: {Stages}",
                    profile, info.Width, info.Height, buffered.Length, result.Jpeg.Length, queuedAhead, timings.FormatStages());

            return result;
        }
    }

    private async Task<MediaPipelineResult> TranscodeImageAsync(Stream buffered, ImageProfile profile, Size? decodeSize,
                                                                PipelineStageTimings timings, CancellationToken ct)
    {
        Image image;
        try
        {
            using (timings.MeasureStage("decode"))
                image = await DecodeFirstFrameAsync(buffered, decodeSize, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Refusing {Profile}, source failed to decode", profile);
            return MediaPipelineResult.FromRejection(MediaPipelineRejection.Unreadable);
        }

        using (image)
        {
            try
            {
                using (timings.MeasureStage("transform"))
                {
                    image.Mutate(x =>
                    {
                        x.AutoOrient();
                        profile.ApplyTransform(x);
                        x.BackgroundColor(Color.White);
                    });

                    StripMetadata(image);
                }

                using (timings.MeasureStage("encode"))
                    return await EncodeWithinBudgetAsync(image, profile, ct);
            }
            catch (ImageProfileRejectedException ex)
            {
                logger.LogInformation("Refusing {Profile}: {Code}", profile, ex.Code);
                return MediaPipelineResult.FromRejection(ex.Code);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Refusing {Profile}, source failed to process", profile);
                return MediaPipelineResult.FromRejection(MediaPipelineRejection.Unreadable);
            }
        }
    }

    private async Task<MediaPipelineResult> EncodeWithinBudgetAsync(Image image, ImageProfile profile, CancellationToken ct)
    {
        foreach (var quality in profile.QualitySteps)
        {
            using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, CreateBaselineEncoder(quality), ct);

            if (profile.MaxOutputBytes is not { } limit || output.Length <= limit)
                return MediaPipelineResult.FromJpeg(output.ToArray());
        }

        logger.LogWarning("Refusing {Profile}, no quality step fits {Limit} bytes", profile, profile.MaxOutputBytes);
        return MediaPipelineResult.FromRejection(MediaPipelineRejection.TooLarge);
    }

    private static JpegEncoder CreateBaselineEncoder(int quality)
    {
        return new JpegEncoder
        {
            Quality = quality,
            ColorType = JpegEncodingColor.YCbCrRatio420,
        };
    }

    private static async Task<bool> TryBufferSourceAsync(Stream source, MemoryStream destination, int maxBytes,
                                                         CancellationToken ct)
    {
        var buffer = new byte[CopyBufferSize];

        while (true)
        {
            var read = await source.ReadAsync(buffer, ct);
            if (read == 0)
                return true;

            if (destination.Length + read > maxBytes)
                return false;

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }

    private async Task<ImageInfo?> TryIdentifyImageAsync(Stream source, ImageProfile profile, CancellationToken ct)
    {
        try
        {
            return await Image.IdentifyAsync(FirstFrameOnly, source, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Refusing {Profile}, source is not a readable image: {Reason}", profile, ex.Message);
            return null;
        }
    }

    // cmyk sources are converted to rgb on decode, so keeping their icc profile would mislabel the output
    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.CicpProfile = null;

        var frame = image.Frames.RootFrame.Metadata;
        frame.ExifProfile = null;
        frame.XmpProfile = null;
        frame.IptcProfile = null;
        frame.IccProfile = null;
        frame.CicpProfile = null;
    }
}
