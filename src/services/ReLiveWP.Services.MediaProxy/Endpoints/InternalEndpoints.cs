using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Services;
using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Endpoints;

public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app, int internalPort)
    {
        var group = app.MapGroup("/internal/v1")
            .AddEndpointFilter(new LocalPortEndpointFilter(internalPort));

        group.MapPost("/process", ProcessImageAsync);
    }

    internal static async Task<Results<FileContentHttpResult, BadRequest<MediaPipelineRejection>, UnprocessableEntity<MediaPipelineRejection>>> ProcessImageAsync(
        string? profile,
        HttpContext context,
        ImagePipelineService pipeline,
        CancellationToken ct)
    {
        if (!ImageProfileParser.TryParseProfile(profile, out var parsed))
            return TypedResults.BadRequest(new MediaPipelineRejection(MediaPipelineRejection.UnknownProfile));

        LiftRequestBodyLimit(context);

        var body = context.Request.Body;
        var declaredBytes = context.Request.ContentLength;
        var result = await pipeline.ProcessImageAsync(body, parsed, ImagePipelineService.MaxSourceBytes, declaredBytes, ct);
        if (result.Jpeg == null)
            return TypedResults.UnprocessableEntity(result.Rejection!);

        return TypedResults.File(result.Jpeg, MediaProfiles.OutputContentType);
    }

    // the pipeline stops reading at its own cap, which is above kestrel's default
    private static void LiftRequestBodyLimit(HttpContext context)
    {
        var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
            limit.MaxRequestBodySize = null;
    }
}
