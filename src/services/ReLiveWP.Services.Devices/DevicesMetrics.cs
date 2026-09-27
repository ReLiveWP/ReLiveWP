using System.Diagnostics;
using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Services.Devices;

public static class DevicesMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    // per instance, sum across replicas
    public static UpDownCounter<long> EventsStreamActive { get; } = Meter.CreateUpDownCounter<long>(
        "relivewp.devices.events_stream.active", "{connection}");

    private static readonly Histogram<double> EventsStreamDuration = Meter.CreateHistogram<double>(
        "relivewp.devices.events_stream.duration", "s", advice: ServiceTelemetry.LongLivedRequestSeconds);

    private static readonly Counter<long> CommandsRequested = Meter.CreateCounter<long>(
        "relivewp.devices.commands.requested", "{command}");

    public static Counter<long> RegistrationLookupFailures { get; } = Meter.CreateCounter<long>(
        "relivewp.devices.registration_lookup.failures", "{lookup}");

    public static void RecordEventsStreamEnd(long startedTimestamp, bool cancelled) =>
        EventsStreamDuration.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>("outcome", cancelled ? "cancelled" : "completed"));

    public static void RecordCommandRequested(string action) =>
        CommandsRequested.Add(1, new KeyValuePair<string, object?>("action", action));
}
