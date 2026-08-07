namespace ReLiveWP.Services.Activity.Services;

public sealed class WindowStream(Stream inner, long length) : Stream
{
    private long position;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => length;

    public override long Position
    {
        get => position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var allowed = (int)Math.Min(count, length - position);
        if (allowed <= 0)
            return 0;

        var read = inner.Read(buffer, offset, allowed);
        position += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var allowed = (int)Math.Min(buffer.Length, length - position);
        if (allowed <= 0)
            return 0;

        var read = await inner.ReadAsync(buffer[..allowed], ct);
        position += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
