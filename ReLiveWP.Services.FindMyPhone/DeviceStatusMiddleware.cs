using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.FindMyPhone;

using FindMyPhoneClient = ReLiveWP.Services.Grpc.FindMyPhone.FindMyPhone.FindMyPhoneClient;

namespace ReLiveWP.Services.FindMyPhone;

public class DeviceStatusMiddleware(RequestDelegate next, ILogger<DeviceStatusMiddleware> logger)
{
    public async Task Invoke(HttpContext context, FindMyPhoneClient findMyPhone)
    {
        await next(context);

        var headers = context.Request.Headers;
        var deviceId = headers["X-WM-DeviceId"].FirstOrDefault();
        if (string.IsNullOrEmpty(deviceId) || context.User?.Identity?.IsAuthenticated != true)
            return;

        try
        {
            var request = new ReportDeviceStatusRequest() { DeviceGuid = deviceId };

            if (int.TryParse(headers["X-WM-BatteryLevel"].FirstOrDefault(), out var battery))
                request.BatteryLevel = battery;

            if (DateTimeOffset.TryParse(headers["X-WM-DeviceTime"].FirstOrDefault(), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var deviceTime))
                request.DeviceTime = deviceTime.ToTimestamp();

            var clientVersion = headers["X-WM-ClientVersion"].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientVersion))
                request.ClientVersion = clientVersion;

            await findMyPhone.ReportDeviceStatusAsync(request, deadline: DateTime.UtcNow.AddSeconds(5));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "ambient device-status update failed for {DeviceId}", deviceId);
        }
    }
}
