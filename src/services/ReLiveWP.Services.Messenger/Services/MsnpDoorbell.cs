using Microsoft.Extensions.Options;
using ReLiveWP.Services.Messenger.Data;

namespace ReLiveWP.Services.Messenger.Services;

public class MsnpDoorbell(
    IMsnpGatewaySessionStore sessions,
    IMsnpDeviceStore devices,
    IHttpClientFactory httpClientFactory,
    IOptions<MessengerOptions> options,
    IConfiguration configuration,
    TimeProvider time,
    ILogger<MsnpDoorbell> logger) : IMsnpDoorbell
{
    public const string HttpClientName = "MsnpDoorbell";

    public async Task RingIfIdleAsync(string sessionId, CancellationToken ct)
    {
        if (await sessions.NotifyAsync(sessionId) > 0)
        {
            MessengerMetrics.RecordDoorbellRing("poll_parked");
            return;
        }

        if (await sessions.FindAsync(sessionId, ct) is not { NotificationUri.Length: > 0 } session)
        {
            MessengerMetrics.RecordDoorbellRing("no_uri");
            return;
        }

        if (ResolvePushUrl(session.NotificationUri) is not { } url)
        {
            MessengerMetrics.RecordDoorbellRing("not_allowed");
            logger.LogWarning("session {SessionId} has a NotificationURI outside our push hosts, not ringing it", sessionId);
            return;
        }

        if (!await sessions.TryClaimDoorbellAsync(sessionId, options.Value.DoorbellRepeatAfter))
        {
            MessengerMetrics.RecordDoorbellRing("debounced");
            logger.LogDebug("not ringing session {SessionId} again yet, it was rung in the last {Window}",
                sessionId, options.Value.DoorbellRepeatAfter);
            return;
        }

        await PostDoorbellAsync(url, $"session {sessionId}", ct);
    }

    public async Task RingDevicesAsync(string userId, CancellationToken ct)
    {
        foreach (var device in await devices.ListDevicesAsync(userId, time.GetUtcNow()))
        {
            if (ResolvePushUrl(device.NotificationUri) is not { } url)
            {
                MessengerMetrics.RecordDoorbellRing("not_allowed");
                continue;
            }

            if (!await devices.TryClaimDoorbellAsync(userId, device.InstanceId, options.Value.DoorbellRepeatAfter))
            {
                MessengerMetrics.RecordDoorbellRing("debounced");
                continue;
            }

            await PostDoorbellAsync(url, $"device {device.InstanceId} of {userId}", ct);
        }
    }

    private async Task PostDoorbellAsync(Uri url, string target, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new ByteArrayContent([]) };
        request.Headers.TryAddWithoutValidation("X-WindowsPhone-Target", "raw");
        request.Headers.TryAddWithoutValidation("X-NotificationClass", "3");

        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, ct);
            MessengerMetrics.RecordDoorbellRing(response.IsSuccessStatusCode ? "rung" : "rejected");
            logger.LogInformation("rang the doorbell for {Target}: {Status}", target, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            MessengerMetrics.RecordDoorbellRing("failed");
            logger.LogWarning(ex, "could not ring the doorbell for {Target}", target);
        }
    }

    private Uri? ResolvePushUrl(string notificationUri) =>
        ResolvePushUrl(notificationUri, options.Value.PushHosts, configuration["Endpoints:Push"]);

    public static Uri? ResolvePushUrl(string notificationUri, IReadOnlySet<string> pushHosts, string? internalPushBase)
    {
        if (!Uri.TryCreate(notificationUri, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !pushHosts.Contains(uri.Host))
            return null;

        if (string.IsNullOrEmpty(internalPushBase))
            return uri;

        return new Uri(new Uri(internalPushBase), uri.PathAndQuery);
    }
}
