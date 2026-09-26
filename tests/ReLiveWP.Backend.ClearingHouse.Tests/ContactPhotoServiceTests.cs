using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class ContactPhotoServiceTests
{
    private static readonly byte[] Tile = [0xFF, 0xD8, 7, 7, 7, 0xFF, 0xD9];

    private sealed class FakeMediaPipeline : HttpMessageHandler
    {
        public Func<HttpResponseMessage> Respond { get; set; } =
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Tile) };

        public List<(string Profile, byte[] Source)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var profile = QueryHelpers.ParseQuery(request.RequestUri!.Query)["profile"].ToString();
            Calls.Add((profile, await request.Content!.ReadAsByteArrayAsync(ct)));
            return Respond();
        }
    }

    private static ContactPhotoService NewService(FakeMediaPipeline pipeline)
    {
        var factory = new NoopHttpClientFactory();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoints:ConnectedServices:Proxy"] = "http://connectedservices",
            })
            .Build();

        var http = new HttpClient(pipeline) { BaseAddress = new Uri("http://mediaproxy:5000") };
        var client = new MediaPipelineClient(http, NullLogger<MediaPipelineClient>.Instance);

        return new(factory, new ConnectedServicesProxy(factory, config), client,
            NullLogger<ContactPhotoService>.Instance);
    }

    private static RemoteContact WithPhoto(byte[]? data, string? url = null, PhotoCrop? crop = null) =>
        new("x", new ContactItem(), PhotoUrl: url, PhotoData: data, PhotoCrop: crop);

    [Fact]
    public async Task An_inline_photo_is_made_into_a_tile_by_the_pipeline()
    {
        var pipeline = new FakeMediaPipeline();

        var result = await NewService(pipeline).ResolveAsync(WithPhoto([1, 2, 3]));

        Assert.Equal(Tile, result);
        var call = Assert.Single(pipeline.Calls);
        Assert.Equal("contact-tile", call.Profile);
        Assert.Equal([1, 2, 3], call.Source);
    }

    [Fact]
    public async Task The_crop_and_its_origin_travel_with_the_photo()
    {
        var pipeline = new FakeMediaPipeline();
        var crop = new PhotoCrop(28, 21, 679, 679, OriginIsBottomLeft: true);

        await NewService(pipeline).ResolveAsync(WithPhoto([1], crop: crop));

        Assert.Equal("contact-tile:28,21,679,679:bottom-left", Assert.Single(pipeline.Calls).Profile);
    }

    [Fact]
    public void A_top_left_crop_carries_no_origin()
    {
        var profile = ContactPhotoService.ChooseTileProfile(new PhotoCrop(10, 0, 400, 400, OriginIsBottomLeft: false));

        Assert.Equal("contact-tile:10,0,400,400", profile);
    }

    [Fact]
    public async Task A_photo_the_pipeline_refuses_yields_no_photo()
    {
        var pipeline = new FakeMediaPipeline
        {
            Respond = () => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = JsonContent.Create(new MediaPipelineRejection(MediaPipelineRejection.Unreadable)),
            },
        };

        Assert.Null(await NewService(pipeline).ResolveAsync(WithPhoto([1, 2, 3, 4, 5])));
    }

    [Fact]
    public async Task The_pipeline_being_down_fails_the_run_instead_of_dropping_the_photo()
    {
        var pipeline = new FakeMediaPipeline { Respond = () => throw new HttpRequestException("connection refused") };

        await Assert.ThrowsAsync<MediaPipelineUnavailableException>(
            () => NewService(pipeline).ResolveAsync(WithPhoto([1, 2, 3])));
    }

    [Fact]
    public async Task A_contact_with_no_photo_yields_null_without_asking_the_pipeline()
    {
        var pipeline = new FakeMediaPipeline();

        Assert.Null(await NewService(pipeline).ResolveAsync(WithPhoto(null)));
        Assert.Empty(pipeline.Calls);
    }

    // these urls arrive inside a provider response, so anything off the provider's own cdn is
    // refused rather than fetched
    [Theory]
    [InlineData("http://lh3.googleusercontent.com/a/x")]
    [InlineData("https://evil.example/steal")]
    [InlineData("https://googleusercontent.com.evil.example/x")]
    [InlineData("https://127.0.0.1/x")]
    [InlineData("file:///etc/passwd")]
    public async Task Refuses_photo_urls_off_the_allowlist(string url)
    {
        Assert.Null(await NewService(new FakeMediaPipeline()).ResolveAsync(WithPhoto(null, url)));
    }

    private sealed class NoopHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("no fetch should have been attempted");
    }
}
