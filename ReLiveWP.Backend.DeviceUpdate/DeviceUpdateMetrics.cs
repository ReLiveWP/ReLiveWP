using System.Diagnostics;
using System.Diagnostics.Metrics;
using ReLiveWP.Backend.DeviceUpdate.Wsup;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Backend.DeviceUpdate;

public static class DeviceUpdateMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    // separate from the request duration so a slow catalog and a slow response build are tellable apart
    private static readonly Histogram<double> CandidateQueryDuration = Meter.CreateHistogram<double>(
        "relivewp.deviceupdate.candidate_query.duration", "s", advice: ServiceTelemetry.RequestSeconds);

    private static readonly Histogram<double> SyncUpdatesDuration = Meter.CreateHistogram<double>(
        "relivewp.deviceupdate.sync_updates.duration", "s", advice: ServiceTelemetry.RequestSeconds);

    private static readonly Histogram<double> ExtendedInfoDuration = Meter.CreateHistogram<double>(
        "relivewp.deviceupdate.extended_info.duration", "s", advice: ServiceTelemetry.RequestSeconds);

    // a device owed nothing looks just like one we're failing to serve, without this
    private static readonly Histogram<int> UpdatesOffered = Meter.CreateHistogram<int>(
        "relivewp.deviceupdate.sync_updates.offered", "{update}");

    private static readonly Counter<long> CandidatesEvaluated = Meter.CreateCounter<long>(
        "relivewp.deviceupdate.candidates.evaluated", "{revision}");

    public static void RecordCandidateQuery(long startedTimestamp, int candidates)
    {
        CandidateQueryDuration.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds);
        CandidatesEvaluated.Add(candidates);
    }

    public static void RecordSyncUpdates(long startedTimestamp, SyncUpdatesResult result)
    {
        var ring = new KeyValuePair<string, object?>("ring", result.Ring.ToString());

        SyncUpdatesDuration.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>("truncated", result.Truncated), ring);

        UpdatesOffered.Record(result.NewCount, new KeyValuePair<string, object?>("section", "new"), ring);
        UpdatesOffered.Record(result.ChangedCount, new KeyValuePair<string, object?>("section", "changed"), ring);
    }

    public static void RecordExtendedInfo(long startedTimestamp, GetExtendedUpdateInfoResult result) =>
        ExtendedInfoDuration.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>("out_of_scope", result.OutOfScopeCount > 0));
}
