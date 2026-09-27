using System.Diagnostics;
using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Push.Nsp;
using ReLiveWP.Services.Push.Pdu;

namespace ReLiveWP.Services.Push;

public static class PushMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    // per instance, sum across replicas. sockets.open counts every accepted socket including ones
    // still handshaking or whose device silently vanished, connections.active only devices we can push to
    public static UpDownCounter<long> SocketsOpen { get; } = Meter.CreateUpDownCounter<long>(
        "relivewp.push.sockets.open", "{socket}");

    public static void ObserveActiveConnections(Func<int> count) =>
        Meter.CreateObservableGauge("relivewp.push.connections.active", count, "{connection}");

    private static readonly Histogram<double> ConnectionDuration = Meter.CreateHistogram<double>(
        "relivewp.push.connection.duration", "s", advice: ServiceTelemetry.LongLivedRequestSeconds);

    public static Histogram<double> KeepAliveInterval { get; } = Meter.CreateHistogram<double>(
        "relivewp.push.keepalive.interval", "s", advice: ServiceTelemetry.LongLivedRequestSeconds);

    private static readonly Counter<long> Pdus = Meter.CreateCounter<long>("relivewp.push.pdus", "{pdu}");

    private static readonly Counter<long> Deliveries = Meter.CreateCounter<long>(
        "relivewp.push.notification.deliveries", "{notification}");

    private static readonly Counter<long> SessionResumes = Meter.CreateCounter<long>(
        "relivewp.push.session.resumes", "{session}");

    public static Counter<long> NotificationsExpired { get; } = Meter.CreateCounter<long>(
        "relivewp.push.notification.expired", "{notification}");

    public static void RecordDisconnect(long startedTimestamp, string reason) =>
        ConnectionDuration.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object>("reason", reason));

    public static void RecordPdu(string direction, PDUCommand command) =>
        Pdus.Add(1,
            new KeyValuePair<string, object>("direction", direction),
            new KeyValuePair<string, object>("command", command.ToString()));

    public static void RecordDelivery(string outcome, NspNotificationClass notificationClass) =>
        Deliveries.Add(1,
            new KeyValuePair<string, object>("outcome", outcome),
            new KeyValuePair<string, object>("class", notificationClass.ToString()));

    public static void RecordSessionResume(bool accepted) =>
        SessionResumes.Add(1, new KeyValuePair<string, object>("outcome", accepted ? "accepted" : "rejected"));
}
