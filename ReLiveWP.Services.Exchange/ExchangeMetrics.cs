using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange;

public static class ExchangeMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    public static Histogram<double> LongPollDuration { get; } = Meter.CreateHistogram<double>(
        "relivewp.eas.longpoll.duration", "s", advice: ServiceTelemetry.LongLivedRequestSeconds);

    // per instance, sum across replicas
    public static UpDownCounter<long> LongPollActive { get; } = Meter.CreateUpDownCounter<long>(
        "relivewp.eas.longpoll.active", "{connection}");

    private static readonly Counter<long> CommandStatus = Meter.CreateCounter<long>(
        "relivewp.eas.command.status", "{response}");

    private static readonly Counter<long> CollectionStatus = Meter.CreateCounter<long>(
        "relivewp.eas.collection.status", "{collection}");

    private static readonly Counter<long> SyncKeyMismatches = Meter.CreateCounter<long>(
        "relivewp.eas.synckey.mismatches", "{event}");

    private static readonly Counter<long> SyncItems = Meter.CreateCounter<long>(
        "relivewp.eas.sync.items", "{item}");

    private static readonly Counter<long> ClientCommandStatus = Meter.CreateCounter<long>(
        "relivewp.eas.sync.client_command.status", "{command}");

    private static readonly Counter<long> ItemsDropped = Meter.CreateCounter<long>(
        "relivewp.eas.sync.items_dropped", "{item}");

    private static readonly Histogram<long> ResponseItems = Meter.CreateHistogram<long>(
        "relivewp.eas.sync.response.items", "{item}",
        advice: new InstrumentAdvice<long> { HistogramBucketBoundaries = [0, 1, 5, 10, 25, 50, 100, 250, 512] });

    private static readonly Counter<long> ChangeNotifications = Meter.CreateCounter<long>(
        "relivewp.eas.change_notifications", "{notification}");

    private static readonly Counter<long> SyncRequestCache = Meter.CreateCounter<long>(
        "relivewp.eas.sync_request_cache", "{operation}");

    private static readonly Counter<long> AttachmentFetches = Meter.CreateCounter<long>(
        "relivewp.eas.attachment.fetches", "{fetch}");

    private static readonly Histogram<long> AttachmentBytes = Meter.CreateHistogram<long>(
        "relivewp.eas.attachment.bytes", "By",
        advice: new InstrumentAdvice<long>
        {
            HistogramBucketBoundaries = [1024, 8192, 65536, 262144, 1048576, 4194304, 10485760, 26214400],
        });

    public static KeyValuePair<string, object?> CommandTag(EasCommand command) => new("command", command.ToString());

    public static void RecordSyncKeyMismatch(EasCommand command, string outcome) =>
        SyncKeyMismatches.Add(1, CommandTag(command), new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordServerItems(string itemClass, SyncCommands? commands)
    {
        var classTag = new KeyValuePair<string, object?>("item_class", itemClass);
        var direction = new KeyValuePair<string, object?>("direction", "server_to_client");

        var added = commands?.Add.Count ?? 0;
        var changed = commands?.Change.Count ?? 0;
        var deleted = commands?.Delete.Count ?? 0;

        if (added > 0) SyncItems.Add(added, direction, classTag, new KeyValuePair<string, object?>("op", "add"));
        if (changed > 0) SyncItems.Add(changed, direction, classTag, new KeyValuePair<string, object?>("op", "change"));
        if (deleted > 0) SyncItems.Add(deleted, direction, classTag, new KeyValuePair<string, object?>("op", "delete"));

        ResponseItems.Record(added + changed + deleted, classTag);
    }

    public static void RecordClientCommand(string itemClass, string op, int status)
    {
        var classTag = new KeyValuePair<string, object?>("item_class", itemClass);
        var opTag = new KeyValuePair<string, object?>("op", op);

        SyncItems.Add(1, new KeyValuePair<string, object?>("direction", "client_to_server"), classTag, opTag);
        ClientCommandStatus.Add(1, classTag, opTag, StatusTag(status));
    }

    public static void RecordItemDropped(string itemClass, string reason) =>
        ItemsDropped.Add(1,
            new KeyValuePair<string, object?>("item_class", itemClass),
            new KeyValuePair<string, object?>("reason", reason));

    public static void RecordChangeNotification(string outcome) =>
        ChangeNotifications.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordSyncRequestCache(string op, string outcome) =>
        SyncRequestCache.Add(1,
            new KeyValuePair<string, object?>("op", op),
            new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordAttachmentFetch(AttachmentResolveStatus status, int? bytes)
    {
        AttachmentFetches.Add(1, new KeyValuePair<string, object?>("outcome", status.ToString()));
        if (bytes is { } served)
            AttachmentBytes.Record(served);
    }

    // every EAS failure is an HTTP 200 carrying a Status element, so this is the only place they count
    public static void RecordResponse(EasCommand command, object response)
    {
        var commandTag = CommandTag(command);

        switch (response)
        {
            case Sync sync:
                if (sync.Status is { } status)
                    Command(status);
                foreach (var collection in sync.Collections?.Items ?? [])
                    CollectionStatus.Add(1, commandTag, StatusTag(collection.Status));
                break;

            case GetItemEstimateResponse estimate:
                foreach (var collection in estimate.Responses)
                    CollectionStatus.Add(1, commandTag, StatusTag(collection.Status));
                break;

            case FolderSync r: Command(r.Status); break;
            case PingResponse r: Command(r.Status); break;
            case SettingsResponse r: Command(r.Status); break;
            case ItemOperationsResponse r: Command(r.Status); break;
            case SendMailResponse r: Command(r.Status); break;
            case SmartReplyResponse r: Command(r.Status); break;
            case SmartForwardResponse r: Command(r.Status); break;
            case FolderCreateResponse r: Command(r.Status); break;
            case FolderUpdateResponse r: Command(r.Status); break;
            case FolderDeleteResponse r: Command(r.Status); break;
            case ProvisionResponse { Status: { } text } when int.TryParse(text, out var parsed): Command(parsed); break;
        }

        void Command(int status) => CommandStatus.Add(1, commandTag, StatusTag(status));
    }

    private static KeyValuePair<string, object?> StatusTag(int status) => new("status", status);
}
