using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Backend.Identity.Tests;

public class AvatarProcessorTests
{
    private static readonly byte[] Original = [0xFF, 0xD8, 5, 1, 2, 0xFF, 0xD9];
    private static readonly byte[] Thumbnail = [0xFF, 0xD8, 5, 0xFF, 0xD9];

    private sealed class FakeMediaPipeline : HttpMessageHandler
    {
        public Func<string, HttpResponseMessage> Respond { get; set; } = profile =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(profile == MediaProfiles.AvatarThumbnailName ? Thumbnail : Original),
            };

        public List<(string Profile, byte[] Source)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var profile = QueryHelpers.ParseQuery(request.RequestUri!.Query)["profile"].ToString();
            Calls.Add((profile, await request.Content!.ReadAsByteArrayAsync(ct)));
            return Respond(profile);
        }
    }

    private static AvatarProcessor NewProcessor(FakeMediaPipeline pipeline)
    {
        var http = new HttpClient(pipeline) { BaseAddress = new Uri("http://mediaproxy:5000") };
        var client = new MediaPipelineClient(http, NullLogger<MediaPipelineClient>.Instance);
        return new AvatarProcessor(client, NullLogger<AvatarProcessor>.Instance);
    }

    private static HttpResponseMessage Refuse(string code, string? detail = null)
    {
        return new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new MediaPipelineRejection(code, detail)),
        };
    }

    [Fact]
    public async Task The_thumbnail_is_made_from_the_processed_original()
    {
        var pipeline = new FakeMediaPipeline();

        var result = await NewProcessor(pipeline).ProcessAsync([1, 2, 3], null);

        Assert.Equal(Original, result.Original);
        Assert.Equal(Thumbnail, result.Thumbnail);
        Assert.Equal("image/jpeg", result.ContentType);

        Assert.Equal(2, pipeline.Calls.Count);
        Assert.Equal("avatar", pipeline.Calls[0].Profile);
        Assert.Equal([1, 2, 3], pipeline.Calls[0].Source);
        Assert.Equal("avatar-thumb", pipeline.Calls[1].Profile);
        Assert.Equal(Original, pipeline.Calls[1].Source);
    }

    [Fact]
    public async Task A_crop_goes_to_the_pipeline_as_a_square()
    {
        var pipeline = new FakeMediaPipeline();

        await NewProcessor(pipeline).ProcessAsync([1], new AvatarCrop(-50, 10, 250));

        Assert.Equal("avatar:-50,10,250,250", pipeline.Calls[0].Profile);
    }

    [Fact]
    public async Task Oversized_uploads_are_rejected_without_asking_the_pipeline()
    {
        var pipeline = new FakeMediaPipeline();

        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor(pipeline).ProcessAsync(new byte[AvatarProcessor.MaxSourceBytes + 1], null));

        Assert.Empty(pipeline.Calls);
    }

    [Fact]
    public async Task Empty_data_is_rejected_without_asking_the_pipeline()
    {
        var pipeline = new FakeMediaPipeline();

        await Assert.ThrowsAsync<AvatarProcessingException>(() => NewProcessor(pipeline).ProcessAsync([], null));

        Assert.Empty(pipeline.Calls);
    }

    [Fact]
    public async Task A_crop_with_a_non_positive_size_is_rejected_without_asking_the_pipeline()
    {
        var pipeline = new FakeMediaPipeline();

        var error = await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor(pipeline).ProcessAsync([1], new AvatarCrop(0, 0, 0)));

        Assert.Equal("crop size must be positive", error.Message);
        Assert.Empty(pipeline.Calls);
    }

    [Theory]
    [InlineData(MediaPipelineRejection.Unreadable, null, "could not read the image")]
    [InlineData(MediaPipelineRejection.TooManyPixels, "9000x9000", "9000x9000 is too large")]
    [InlineData(MediaPipelineRejection.CropOutside, null, "crop rectangle falls outside the image")]
    [InlineData(MediaPipelineRejection.TooLarge, null, "image is too large")]
    [InlineData("something-new", null, "could not process the image (something-new)")]
    public async Task A_refusal_keeps_the_message_users_already_see(string code, string? detail, string message)
    {
        var pipeline = new FakeMediaPipeline { Respond = _ => Refuse(code, detail) };

        var error = await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor(pipeline).ProcessAsync([1, 2, 3], null));

        Assert.Equal(message, error.Message);
    }

    [Fact]
    public async Task The_pipeline_being_down_is_not_the_users_fault()
    {
        var pipeline = new FakeMediaPipeline { Respond = _ => throw new HttpRequestException("connection refused") };

        await Assert.ThrowsAsync<MediaPipelineUnavailableException>(
            () => NewProcessor(pipeline).ProcessAsync([1, 2, 3], null));
    }
}
