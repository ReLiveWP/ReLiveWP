using Grpc.Core;

namespace ReLiveWP.Backend.Mailbox.Tests;

internal sealed class CollectingStreamWriter<T> : IServerStreamWriter<T>
{
    public List<T> Written { get; } = [];

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Written.Add(message);
        return Task.CompletedTask;
    }
}
