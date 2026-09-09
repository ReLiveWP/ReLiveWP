using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Services.FindMyPhone;

public static class FindMyPhoneMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    private static readonly Counter<long> AmbientStatusUpdates = Meter.CreateCounter<long>(
        "relivewp.findmyphone.ambient_status.updates", "{update}");

    // the device protocol answers HTTP 200 with the real result in ResponseCode
    private static readonly Counter<long> DeviceResponses = Meter.CreateCounter<long>(
        "relivewp.findmyphone.device_responses", "{response}");

    public static Histogram<long> CommandStatusBatchSize { get; } = Meter.CreateHistogram<long>(
        "relivewp.findmyphone.command_status.batch_size", "{report}");

    public static void RecordAmbientStatusUpdate(bool succeeded) =>
        AmbientStatusUpdates.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "ok" : "failed"));

    public static void RecordDeviceResponse(string endpoint, int code) =>
        DeviceResponses.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("code", code));
}
