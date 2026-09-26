using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.Activity.Tests;

public record PipelineCall(string Profile, byte[] Source);

public class FakeMediaPipeline : HttpMessageHandler
{
    public Func<PipelineCall, HttpResponseMessage> OnProcess { get; set; } =
        call => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(call.Source.Reverse().ToArray()) };

    public List<PipelineCall> Calls { get; } = [];

    public MediaPipelineClient CreateClient()
    {
        var http = new HttpClient(this) { BaseAddress = new Uri("http://mediaproxy:5000") };
        return new MediaPipelineClient(http, NullLogger<MediaPipelineClient>.Instance);
    }

    public static HttpResponseMessage RefuseWithCode(string code)
    {
        return new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new MediaPipelineRejection(code)),
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var query = QueryHelpers.ParseQuery(request.RequestUri!.Query);
        var source = await request.Content!.ReadAsByteArrayAsync(ct);
        var call = new PipelineCall(query["profile"].ToString(), source);

        Calls.Add(call);
        return OnProcess(call);
    }
}
