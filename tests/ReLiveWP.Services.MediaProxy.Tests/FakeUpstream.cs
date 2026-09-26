using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class FakeUpstream : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> routes = [];

    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public void Respond(string url, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        routes[url] = respond;
    }

    public void RespondWithBytes(string url, byte[] body, string contentType)
    {
        Respond(url, (_, _) => Task.FromResult(CreateBytesResponse(body, contentType)));
    }

    public void RespondWithRedirect(string url, string location)
    {
        Respond(url, (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
            return Task.FromResult(response);
        });
    }

    public static HttpResponseMessage CreateBytesResponse(byte[] body, string contentType)
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    public static HttpResponseMessage CreateStreamResponse(Stream body, long? declaredLength = null)
    {
        var content = new StreamContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Headers.ContentLength = declaredLength;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Enqueue(request);

        if (routes.TryGetValue(request.RequestUri!.AbsoluteUri, out var respond))
            return respond(request, ct);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public class StallingStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => 0; set => throw new NotSupportedException(); }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return 0;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
