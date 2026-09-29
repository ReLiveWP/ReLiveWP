using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat;

public static class ChatMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    private static readonly Counter<long> Messages = Meter.CreateCounter<long>(
        "relivewp.chat.messages", "{message}");

    private static readonly Histogram<long> MessageSize = Meter.CreateHistogram<long>(
        "relivewp.chat.message.size", "By");

    private static readonly Counter<long> StoredMessages = Meter.CreateCounter<long>(
        "relivewp.chat.messages.stored", "{message}");

    private static readonly Counter<long> DeliveriesQueued = Meter.CreateCounter<long>(
        "relivewp.chat.deliveries.queued", "{delivery}");

    private static readonly Counter<long> PresenceChanges = Meter.CreateCounter<long>(
        "relivewp.chat.presence.changes", "{change}");

    private static readonly Counter<long> PresenceDeferrals = Meter.CreateCounter<long>(
        "relivewp.chat.presence.deferred", "{change}");

    private static readonly Counter<long> EndpointEvents = Meter.CreateCounter<long>(
        "relivewp.chat.endpoint.events", "{event}");

    private static readonly Counter<long> AudienceRefreshes = Meter.CreateCounter<long>(
        "relivewp.chat.audience.refreshes", "{refresh}");

    public static void RecordMessage(MessageKind kind, SendOutcome outcome, int bytes)
    {
        var kindTag = new KeyValuePair<string, object?>("kind", kind.ToString());
        Messages.Add(1, kindTag, new KeyValuePair<string, object?>("outcome", outcome.ToString()));
        MessageSize.Record(bytes, kindTag);
    }

    public static void RecordStoredMessages(string storedEvent, int count) =>
        StoredMessages.Add(count, new KeyValuePair<string, object?>("event", storedEvent));

    public static void RecordDeliveryQueued(ChatDelivery.KindOneofCase kind) =>
        DeliveriesQueued.Add(1, new KeyValuePair<string, object?>("kind", kind.ToString()));

    public static void RecordPresenceChange(PresenceStatus shown, bool devicesOnly) =>
        PresenceChanges.Add(1,
            new KeyValuePair<string, object?>("shown", shown.ToString()),
            new KeyValuePair<string, object?>("devices_only", devicesOnly));

    public static void RecordPresenceDeferred() => PresenceDeferrals.Add(1);

    public static void RecordEndpointEvent(string endpointEvent) =>
        EndpointEvents.Add(1, new KeyValuePair<string, object?>("event", endpointEvent));

    public static void RecordAudienceRefresh(string outcome) =>
        AudienceRefreshes.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
