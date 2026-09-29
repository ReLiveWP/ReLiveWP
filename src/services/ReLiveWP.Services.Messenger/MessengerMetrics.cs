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

    private static readonly Counter<long> SdgsReceived = Meter.CreateCounter<long>(
        "relivewp.messenger.sdg.received", "{sdg}");

    private static readonly Counter<long> P2pSteps = Meter.CreateCounter<long>(
        "relivewp.messenger.p2p.steps", "{sdg}");

    private static readonly Counter<long> DeliveriesWritten = Meter.CreateCounter<long>(
        "relivewp.messenger.deliveries.written", "{delivery}");

    private static readonly Histogram<double> DeliveryQueueTime = Meter.CreateHistogram<double>(
        "relivewp.messenger.delivery.queue_time", "s", advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 120, 300],
        });

    private static readonly Counter<long> DoorbellRings = Meter.CreateCounter<long>(
        "relivewp.messenger.doorbell.rings", "{ring}");

    private static readonly Counter<long> PollsRequeued = Meter.CreateCounter<long>(
        "relivewp.messenger.polls.requeued", "{poll}");

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

    public static void RecordSdgReceived(string messageType, string outcome) =>
        SdgsReceived.Add(1,
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordP2pStep(MsnpP2pStep step) =>
        P2pSteps.Add(1, new KeyValuePair<string, object?>("step", step.Step));

    public static void RecordDeliveryWritten(string kind, TimeSpan? queueTime)
    {
        var kindTag = new KeyValuePair<string, object?>("kind", kind);
        DeliveriesWritten.Add(1, kindTag);
        if (queueTime is { } waited)
            DeliveryQueueTime.Record(waited.TotalSeconds, kindTag);
    }

    public static void RecordDoorbellRing(string outcome) =>
        DoorbellRings.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordPollRequeued() => PollsRequeued.Add(1);
}
