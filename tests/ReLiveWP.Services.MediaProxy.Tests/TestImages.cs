using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace ReLiveWP.Services.MediaProxy.Tests;

internal static class TestImages
{
    private const int PngWidthOffset = 16;
    private const int PngHeightOffset = 20;
    private const int PngHeaderChunkStart = 12;
    private const int PngHeaderChunkLength = 17;

    private const int TiffFirstDirectoryOffset = 4;
    private const int TiffEntryCountBytes = 2;
    private const int TiffEntryBytes = 12;
    private const int TiffEntryValueOffset = 8;
    private const ushort TiffCompressionTag = 259;

    public static MemoryStream EncodeImage(Image image, IImageEncoder encoder)
    {
        var stream = new MemoryStream();
        image.Save(stream, encoder);
        stream.Position = 0;
        return stream;
    }

    public static MemoryStream CreateJpeg(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        return EncodeImage(image, new JpegEncoder());
    }

    public static MemoryStream CreatePng(int width, int height, Color fill)
    {
        using var image = new Image<Rgba32>(width, height, fill.ToPixel<Rgba32>());
        return EncodeImage(image, new PngEncoder());
    }

    public static MemoryStream CreateAnimatedGif(params Color[] frameColours)
    {
        using var gif = new Image<Rgba32>(32, 32, frameColours[0].ToPixel<Rgba32>());

        foreach (var colour in frameColours.Skip(1))
        {
            using var frame = new Image<Rgba32>(32, 32, colour.ToPixel<Rgba32>());
            gif.Frames.AddFrame(frame.Frames.RootFrame);
        }

        return EncodeImage(gif, new GifEncoder());
    }

    public static MemoryStream CreateTiffWithCompression(ushort compression)
    {
        using var image = new Image<Rgba32>(16, 16, Color.Red.ToPixel<Rgba32>());
        using var encoded = EncodeImage(image, new TiffEncoder());
        var bytes = encoded.ToArray();

        var directory = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(TiffFirstDirectoryOffset));
        var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(directory));

        for (var i = 0; i < entryCount; i++)
        {
            var entry = directory + TiffEntryCountBytes + i * TiffEntryBytes;
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(entry)) != TiffCompressionTag)
                continue;

            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(entry + TiffEntryValueOffset), compression);
            return new MemoryStream(bytes);
        }

        throw new InvalidOperationException("the encoder wrote no compression tag");
    }

    // a real 1x1 png whose header claims another size, so only a decode would notice the lie
    public static MemoryStream CreatePngClaimingSize(int width, int height)
    {
        using var small = CreatePng(1, 1, Color.Black);
        var bytes = small.ToArray();

        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(PngWidthOffset), width);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(PngHeightOffset), height);

        var crc = ComputePngCrc(bytes.AsSpan(PngHeaderChunkStart, PngHeaderChunkLength));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(PngHeaderChunkStart + PngHeaderChunkLength), crc);

        return new MemoryStream(bytes);
    }

    public static Rgba32 ReadCentrePixel(byte[] encoded)
    {
        using var image = Image.Load<Rgba32>(encoded);
        return image[image.Width / 2, image.Height / 2];
    }

    private static uint ComputePngCrc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }
}

internal sealed class ZeroFilledStream(long length) : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = length - Position;
        var read = (int)Math.Min(count, remaining);

        Array.Clear(buffer, offset, read);
        Position += read;
        return read;
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
