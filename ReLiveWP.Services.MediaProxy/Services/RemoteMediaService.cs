using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Services;

public sealed class RemoteMediaService(HttpClient http, ImagePipelineService pipeline, ILogger<RemoteMediaService> logger)
{
    public const int MaxRemoteBytes = 20 * 1024 * 1024;
    public const string UserAgent = "ReLiveWP-MediaProxy/1.0";

    private static readonly TimeSpan ConnectionLifetime = TimeSpan.FromMinutes(5);

    private static readonly string[] AcceptedTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif", "image/*;q=0.8"];

    private readonly SingleFlight<(string Source, MediaSize Size), RemoteMediaResult> inFlight = new();

    public TimeSpan FetchDeadline { get; init; } = TimeSpan.FromSeconds(30);

    // not from the factory, its one breaker per client name would let a few dead instances block every host
    public static HttpClient CreateRemoteClient(IOutboundAddressPolicy policy)
    {
        var handler = CreateRemoteHandler(policy);
        var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return http;
    }

    internal static SocketsHttpHandler CreateRemoteHandler(IOutboundAddressPolicy policy)
    {
        var handler = OutboundAddressPolicyExtensions.CreateGuardedHandler(policy);
        handler.PooledConnectionLifetime = ConnectionLifetime;
        return handler;
    }

    public Task<RemoteMediaResult> RenderMediaAsync(Uri source, MediaSize size, CancellationToken ct = default)
    {
        var key = (source.AbsoluteUri, size);
        return inFlight.RunOnceAsync(key, () => FetchAndRenderAsync(source, size), ct);
    }

    private async Task<RemoteMediaResult> FetchAndRenderAsync(Uri source, MediaSize size)
    {
        using var deadline = new CancellationTokenSource(FetchDeadline);

        try
        {
            using var response = await ExternalRequestGuard.GetFollowingRedirectsAsync(
                http, source, AcceptedTypes, HttpCompletionOption.ResponseHeadersRead, deadline.Token);

            if (response is not { IsSuccessStatusCode: true })
            {
                logger.LogInformation("Upstream gave {Status} for {Source}", (int?)response?.StatusCode, source);
                return RemoteMediaResult.UpstreamFailed;
            }

            var declaredBytes = response.Content.Headers.ContentLength;
            await using var body = await response.Content.ReadAsStreamAsync(deadline.Token);
            var profile = new ThumbnailProfile(MediaSizes.MaxEdgeFor(size));
            var result = await pipeline.ProcessImageAsync(body, profile, MaxRemoteBytes, declaredBytes, deadline.Token);

            if (result.Jpeg == null)
                return RemoteMediaResult.FromRejection(result.Rejection!.Code);

            return RemoteMediaResult.FromJpeg(result.Jpeg);
        }
        catch (Exception ex) when (IsUpstreamFailure(ex, deadline))
        {
            logger.LogInformation(ex, "Could not fetch {Source}", source);
            return RemoteMediaResult.UpstreamFailed;
        }
    }

    private static bool IsUpstreamFailure(Exception ex, CancellationTokenSource deadline)
    {
        if (ex is OperationCanceledException)
            return deadline.IsCancellationRequested;

        return ex is HttpRequestException or IOException;
    }
}
