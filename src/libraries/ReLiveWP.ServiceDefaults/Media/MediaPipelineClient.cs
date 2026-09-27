using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ReLiveWP.ServiceDefaults.Media;

public sealed class MediaPipelineClient(HttpClient http, ILogger<MediaPipelineClient> logger)
{
    public const string ProcessPath = "internal/v1/process";

    private static readonly MediaTypeHeaderValue OctetStream = new("application/octet-stream");

    public Task<MediaPipelineResult> ProcessImageAsync(Stream source, string profile, CancellationToken ct = default)
    {
        return ProcessImageAsync(source, profile, null, ct);
    }

    public async Task<MediaPipelineResult> ProcessImageAsync(Stream source, string profile, long? sourceLength,
                                                             CancellationToken ct = default)
    {
        var path = $"{ProcessPath}?profile={Uri.EscapeDataString(profile)}";

        using var content = new StreamContent(source);
        content.Headers.ContentType = OctetStream;
        if (sourceLength is { } length)
            content.Headers.ContentLength = length;

        try
        {
            using var response = await http.PostAsync(path, content, ct);
            if (response.IsSuccessStatusCode)
            {
                var jpeg = await response.Content.ReadAsByteArrayAsync(ct);
                return MediaPipelineResult.FromJpeg(jpeg);
            }

            var rejection = await ReadRejectionAsync(response, ct);
            if (rejection == null)
            {
                logger.LogWarning("Media pipeline answered {Profile} with {Status}", profile, (int)response.StatusCode);
                return MediaPipelineResult.Unavailable;
            }

            logger.LogInformation("Media pipeline refused {Profile}: {Code} {Detail}", profile, rejection.Code, rejection.Detail);
            return new MediaPipelineResult(null, rejection);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Media pipeline unavailable for {Profile}", profile);
            return MediaPipelineResult.Unavailable;
        }
    }

    private static async Task<MediaPipelineRejection?> ReadRejectionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity))
            return null;

        if (response.Content.Headers.ContentType?.MediaType != "application/json")
            return null;

        try
        {
            return await response.Content.ReadFromJsonAsync<MediaPipelineRejection>(ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
