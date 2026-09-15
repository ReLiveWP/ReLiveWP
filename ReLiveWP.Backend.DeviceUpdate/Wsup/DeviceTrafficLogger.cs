using System.Text;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public class DeviceTrafficLogger(RequestDelegate next, ILogger<DeviceTrafficLogger> logger, IConfiguration config)
{
    public const string SummaryKey = "DeviceUpdate.Summary";

    private static readonly Dictionary<string, int> Sequences = [];
    private static readonly object SequenceLock = new();

    private readonly string? capturePath = config["Capture:Path"];

    public async Task InvokeAsync(HttpContext context)
    {
        var action = context.Request.Headers["SOAPAction"].FirstOrDefault()?.Trim('"');
        if (action is null)
        {
            await next(context);
            return;
        }

        var method = action[(action.LastIndexOf('/') + 1)..];

        if (string.IsNullOrEmpty(capturePath))
        {
            await next(context);
            LogSummary(context, method);
            return;
        }

        await CaptureAsync(context, method);
    }

    private async Task CaptureAsync(HttpContext context, string method)
    {
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body, leaveOpen: true).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }

        LogSummary(context, method);
        WriteCapture(context, method, requestBody, Encoding.UTF8.GetString(buffer.ToArray()));
    }

    private void LogSummary(HttpContext context, string method)
    {
        var device = DeviceKey(context);

        context.Items.TryGetValue(SummaryKey, out var summary);

        switch (summary)
        {
            case SyncUpdatesResult sync:
                logger.LogInformation(
                    "SyncUpdates {Device} [{Ring}]: reports installed={Installed} cached={Cached} -> offered={Offered} (new={New} install={Install} bundle={Bundle}) blocked={Blocked} outOfScope={OutOfScope} truncated={Truncated}",
                    device, sync.Ring, sync.ReportedInstalled, sync.ReportedCached, sync.OfferedCount,
                    sync.NewCount, sync.InstallCount, sync.BundleCount,
                    sync.BlockedCount, sync.OutOfScopeCount, sync.Truncated);
                break;

            case GetExtendedUpdateInfoResult extended:
                logger.LogInformation(
                    "GetExtendedUpdateInfo {Device}: requested={Requested} -> fragments={Fragments} fileLocations={Locations} outOfScope={OutOfScope}",
                    device, extended.RequestedCount, extended.FragmentCount,
                    extended.FileLocationCount, extended.OutOfScopeCount);
                break;

            default:
                logger.LogInformation("{Method} {Device}: {Status}", method, device, context.Response.StatusCode);
                break;
        }
    }

    private void WriteCapture(HttpContext context, string method, string request, string response)
    {
        var session = Path.Combine(capturePath!, SessionKey(context));
        Directory.CreateDirectory(session);

        var sequence = NextSequence(session);
        File.WriteAllText(Path.Combine(session, $"{sequence}_{method}Request.xml"), request);
        File.WriteAllText(Path.Combine(session, $"{sequence}_{method}Response.xml"), response);
    }

    private static int NextSequence(string session)
    {
        lock (SequenceLock)
        {
            if (!Sequences.TryGetValue(session, out var sequence))
                sequence = Directory.EnumerateFiles(session, "*Request.xml").Count();

            sequence++;
            Sequences[session] = sequence;
            return sequence;
        }
    }

    private static string DeviceKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string SessionKey(HttpContext context)
    {
        var address = DeviceKey(context).Replace(':', '-').Replace('.', '-');
        return $"{address}_{DateTime.UtcNow:yyyyMMdd}";
    }
}
