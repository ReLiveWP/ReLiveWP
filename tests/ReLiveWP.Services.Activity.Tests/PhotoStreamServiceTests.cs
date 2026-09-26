using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Providers.Bluesky;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Activity.Tests;

public class PhotoStreamServiceTests
{
    private const string UserId = "user-1";
    private const string ResourceRef = "webdav+UmVMaXZlV1AvSU1HXzAwMDEuanBn";
    private const string SourceUrl = "https://files.example/IMG_0001.jpg";
    private const string OwnDid = "did:plc:amyamyamyamyamyamyamy";
    private const string StrangerDid = "did:plc:strangerstrangerstra";
    private const string BlobCid = "bafkreiacbosk5hldxy";

    private static readonly byte[] Original = [0xFF, 0xD8, 1, 2, 3, 0xFF, 0xD9];

    private sealed class FakeUpstream : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;

            var content = new ByteArrayContent(Original);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeMediaProxy : HttpMessageHandler
    {
        public static readonly byte[] Proxied = [0xFF, 0xD8, 9, 9, 9, 0xFF, 0xD9];

        public List<Uri> Requests { get; } = [];

        public HttpStatusCode? FailWith { get; set; }

        public bool IsDown { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!);

            if (IsDown)
                throw new HttpRequestException("connection refused");

            if (FailWith is { } status)
                return Task.FromResult(new HttpResponseMessage(status));

            var content = new ByteArrayContent(Proxied);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class Harness : IDisposable
    {
        public FakeUpstream Upstream { get; } = new();
        public FakeMediaPipeline Pipeline { get; } = new();
        public FakeMediaProxy MediaProxy { get; } = new();
        public FakeSkyDriveClient SkyDrive { get; } = new();
        public FakeConnectedServicesClient ConnectedServices { get; } = new();
        public FakeMailboxStoreClient Mailbox { get; } = new();
        public ThumbnailService Thumbnails { get; }
        public PhotoStreamService Photos { get; }

        public Harness(int resizeTo = 0, MediaProxyUrlSigner? signer = null)
        {
            SkyDrive.OnGetPhotoContent = _ => new GetPhotoContentReply
            {
                Exists = true,
                Url = SourceUrl,
                ContentType = "image/jpeg",
                ResizeTo = resizeTo,
            };

            ConnectedServices.OnGetConnections = _ => [new Connection { Service = "atproto", UserId = OwnDid }];
            Mailbox.OnResolveAuthorsToContacts = _ => new ResolveAuthorsToContactsResponse();

            var library = new PhotoLibraryService(SkyDrive, NullLogger<PhotoLibraryService>.Instance);
            Thumbnails = new ThumbnailService(Pipeline.CreateClient(), NullLogger<ThumbnailService>.Instance);

            var albums = new SocialAlbumsService([new BlueskyAlbumProvider(null!, TestCache.New(), NullLoggerFactory.Instance)]);
            var activity = new ActivityProviderService(null!, [], [], ConnectedServices, Mailbox, TestCache.New(),
                                                       NullLogger<ActivityProviderService>.Instance);
            var social = new SocialAlbumService(albums, new ConnectionLookupService(ConnectedServices), activity,
                                                NullLogger<SocialAlbumService>.Instance);
            var viewer = new FileViewerService(new FakeUserClient(), TestCache.New(), NullLogger<FileViewerService>.Instance);

            var proxyHttp = new HttpClient(MediaProxy) { BaseAddress = new Uri("http://mediaproxy:10021") };
            var proxy = new MediaProxyClient(proxyHttp, signer ?? TestMediaProxy.CreateSigner(),
                                             NullLogger<MediaProxyClient>.Instance);

            Photos = new PhotoStreamService(library, albums, social, viewer, Thumbnails, proxy,
                                            new FakeHttpClientFactory(Upstream));
        }

        public async Task<(bool Written, HttpContext Context, byte[] Body)> RequestAsync(string resourceRef, int size)
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "test")),
            };

            using var body = new MemoryStream();
            context.Response.Body = body;

            var written = await Photos.WriteAsync(context, "wmphotos", resourceRef, size);
            return (written, context, body.ToArray());
        }

        public async Task<(HttpContext Context, byte[] Body)> RequestThumbnailAsync(int size)
        {
            var (written, context, body) = await RequestAsync(ResourceRef, size);
            Assert.True(written);
            return (context, body);
        }

        public void Dispose() => Thumbnails.Dispose();
    }

    [Theory]
    [InlineData(96, "avatar", "feed_thumbnail")]
    [InlineData(176, "thumb", "feed_thumbnail")]
    [InlineData(800, "full", "feed_fullsize")]
    [InlineData(0, "full", "feed_fullsize")]
    public async Task ASocialPhotoComesThroughTheMediaProxy(int requested, string proxySize, string rendition)
    {
        using var harness = new Harness();

        var (written, context, body) = await harness.RequestAsync($"atproto+{OwnDid}+{BlobCid}", requested);

        Assert.True(written);
        Assert.Equal(FakeMediaProxy.Proxied, body);
        Assert.Equal("image/jpeg", context.Response.ContentType);
        Assert.Equal(0, harness.Upstream.Requests);

        var request = Assert.Single(harness.MediaProxy.Requests);
        Assert.Equal("mediaproxy", request.Host);
        Assert.StartsWith($"/v1/{proxySize}/", request.AbsolutePath);

        var encodedSource = request.AbsolutePath.Split('/')[4].Replace(".jpg", "");
        var source = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedSource));
        Assert.Equal($"https://cdn.bsky.app/img/{rendition}/plain/{OwnDid}/{BlobCid}@jpeg", source);
    }

    [Fact]
    public async Task ASocialPhotoIsFetchedFromTheProxyOncePerSize()
    {
        using var harness = new Harness();
        var resourceRef = $"atproto+{OwnDid}+{BlobCid}";

        var (_, _, first) = await harness.RequestAsync(resourceRef, 176);
        var (written, context, second) = await harness.RequestAsync(resourceRef, 176);
        await harness.RequestAsync(resourceRef, 800);

        Assert.True(written);
        Assert.Equal(first, second);
        Assert.Equal("image/jpeg", context.Response.ContentType);
        Assert.Equal(2, harness.MediaProxy.Requests.Count);
    }

    [Fact]
    public async Task AProxyFailureIsPassedOnAndNotCached()
    {
        using var harness = new Harness();
        var resourceRef = $"atproto+{OwnDid}+{BlobCid}";
        harness.MediaProxy.FailWith = HttpStatusCode.UnsupportedMediaType;

        var (written, context, body) = await harness.RequestAsync(resourceRef, 176);
        harness.MediaProxy.FailWith = null;
        var (_, _, retried) = await harness.RequestAsync(resourceRef, 176);

        Assert.True(written);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, context.Response.StatusCode);
        Assert.Empty(body);
        Assert.Equal(FakeMediaProxy.Proxied, retried);
        Assert.Equal(2, harness.MediaProxy.Requests.Count);
    }

    [Fact]
    public async Task TheProxyBeingDownIsABadGateway()
    {
        using var harness = new Harness();
        harness.MediaProxy.IsDown = true;

        var (written, context, _) = await harness.RequestAsync($"atproto+{OwnDid}+{BlobCid}", 176);

        Assert.True(written);
        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
    }

    [Fact]
    public async Task ASocialPhotoTheViewerMayNotSeeNeverReachesTheProxy()
    {
        using var harness = new Harness();

        var (written, _, _) = await harness.RequestAsync($"atproto+{StrangerDid}+{BlobCid}", 176);

        Assert.False(written);
        Assert.Empty(harness.MediaProxy.Requests);
    }

    [Fact]
    public async Task WithoutAProxyKeyASocialPhotoIsNotServed()
    {
        using var harness = new Harness(signer: TestMediaProxy.Unconfigured);

        var (written, _, _) = await harness.RequestAsync($"atproto+{OwnDid}+{BlobCid}", 176);

        Assert.False(written);
        Assert.Empty(harness.MediaProxy.Requests);
        Assert.Equal(0, harness.Upstream.Requests);
    }

    [Fact]
    public async Task ServesTheThumbnailFromTheMediaPipeline()
    {
        using var harness = new Harness(resizeTo: 176);

        var (context, body) = await harness.RequestThumbnailAsync(176);

        Assert.Equal("image/jpeg", context.Response.ContentType);
        Assert.Equal(Original.Reverse(), body);

        var call = Assert.Single(harness.Pipeline.Calls);
        Assert.Equal("thumb:176", call.Profile);
        Assert.Equal(Original, call.Source);
    }

    [Fact]
    public async Task ACachedThumbnailNeverTouchesTheUpstream()
    {
        using var harness = new Harness(resizeTo: 176);

        var (_, first) = await harness.RequestThumbnailAsync(176);
        var (_, second) = await harness.RequestThumbnailAsync(176);

        Assert.Equal(first, second);
        Assert.Equal(1, harness.Upstream.Requests);
        Assert.Single(harness.Pipeline.Calls);
        Assert.Equal(2, harness.SkyDrive.GetPhotoContentCalls);
    }

    [Fact]
    public async Task ServesTheOriginalWhenThePipelineIsDown()
    {
        using var harness = new Harness(resizeTo: 176);
        harness.Pipeline.OnProcess = _ => throw new HttpRequestException("connection refused");

        var (context, body) = await harness.RequestThumbnailAsync(176);

        Assert.Equal("image/jpeg", context.Response.ContentType);
        Assert.Equal(Original, body);
    }

    [Fact]
    public async Task ServesTheOriginalWhenThePipelineRefuses()
    {
        using var harness = new Harness(resizeTo: 176);
        harness.Pipeline.OnProcess = _ => FakeMediaPipeline.RefuseWithCode(MediaPipelineRejection.Unreadable);

        var (_, body) = await harness.RequestThumbnailAsync(176);

        Assert.Equal(Original, body);
        Assert.False(harness.Thumbnails.TryGetCachedThumbnail(UserId, ResourceRef, 176, out _));
    }

    [Fact]
    public async Task ARenditionTheProviderAlreadySizedSkipsThePipeline()
    {
        using var harness = new Harness(resizeTo: 0);

        var (_, body) = await harness.RequestThumbnailAsync(176);

        Assert.Equal(Original, body);
        Assert.Empty(harness.Pipeline.Calls);
    }
}
