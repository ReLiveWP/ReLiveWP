using System.Diagnostics;
using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger;

public static class MessengerMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    private static readonly Counter<long> GatewayRequests = Meter.CreateCounter<long>(
        "relivewp.messenger.gateway.requests", "{request}");

    private static readonly Histogram<double> PollWait = Meter.CreateHistogram<double>(
        "relivewp.messenger.poll.wait", "s", advice: ServiceTelemetry.LongLivedRequestSeconds);

    // per instance, sum across replicas
    public static UpDownCounter<long> PollsActive { get; } = Meter.CreateUpDownCounter<long>(
        "relivewp.messenger.polls.active", "{request}");

    private static readonly Counter<long> OutboxEnqueued = Meter.CreateCounter<long>(
        "relivewp.messenger.outbox.enqueued", "{command}");

    private static readonly Histogram<long> OutboxDepth = Meter.CreateHistogram<long>(
        "relivewp.messenger.outbox.depth", "{command}");

    private static readonly Counter<long> SsoVerifications = Meter.CreateCounter<long>(
        "relivewp.messenger.sso.verifications", "{attempt}");

    public static void RecordGatewayRequest(string action, string outcome) =>
        GatewayRequests.Add(1,
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordPollWait(long startedTimestamp, string outcome) =>
        PollWait.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordOutboxEnqueue(IEnumerable<MsnpCommand> commands, long depthAfterPush)
    {
        foreach (var command in commands)
            OutboxEnqueued.Add(1, new KeyValuePair<string, object?>("verb", command.Verb.ToUpperInvariant()));

        OutboxDepth.Record(depthAfterPush);
    }

    public static void RecordSsoVerification(string outcome) =>
        SsoVerifications.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
