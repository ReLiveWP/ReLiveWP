using System.Diagnostics.Metrics;
using ReLiveWP.Backend.Skybox.Commands;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Backend.Skybox;

public static class SkyboxMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    private static readonly Counter<long> CommandsDispatched = Meter.CreateCounter<long>(
        "relivewp.skybox.commands.dispatched", "{command}");

    // redis pub/sub has no persistence: a status published to zero receivers is gone
    public static Histogram<long> StatusReceivers { get; } = Meter.CreateHistogram<long>(
        "relivewp.skybox.command_status.receivers", "{subscriber}",
        advice: new InstrumentAdvice<long> { HistogramBucketBoundaries = [0, 1, 2, 5, 10] });

    // per instance, sum across replicas
    public static UpDownCounter<long> StatusSubscribersActive { get; } = Meter.CreateUpDownCounter<long>(
        "relivewp.skybox.command_status.subscribers.active", "{subscriber}");

    private static readonly Counter<long> StatusReports = Meter.CreateCounter<long>(
        "relivewp.skybox.command_status.reports", "{report}");

    public static void RecordCommandDispatched(DeviceCommandAction action) =>
        CommandsDispatched.Add(1, new KeyValuePair<string, object?>("action", action.ToString()));

    public static void RecordStatusReport(string outcome) =>
        StatusReports.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
