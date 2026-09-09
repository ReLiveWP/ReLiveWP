using System.Diagnostics.Metrics;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Backend.Mailbox;

public static class MailboxMetrics
{
    private static readonly Meter Meter = ServiceTelemetry.Meter;

    private static readonly Counter<long> DedupHits = Meter.CreateCounter<long>(
        "relivewp.mailbox.item.dedup_hits", "{item}");

    private static readonly Counter<long> ChangeEvents = Meter.CreateCounter<long>(
        "relivewp.mailbox.change_events", "{event}");

    public static Counter<long> PublishFailures { get; } = Meter.CreateCounter<long>(
        "relivewp.mailbox.change_notifications.publish_failures", "{event}");

    public static Counter<long> SyncStateWriteRetries { get; } = Meter.CreateCounter<long>(
        "relivewp.mailbox.syncstate.write_retries", "{retry}");

    private static readonly Counter<long> AccountEvents = Meter.CreateCounter<long>(
        "relivewp.mailbox.account_events", "{event}");

    private static readonly Counter<long> ContactLinks = Meter.CreateCounter<long>(
        "relivewp.mailbox.contact_links", "{lookup}");

    private static readonly Counter<long> MeContactMirror = Meter.CreateCounter<long>(
        "relivewp.mailbox.me_contact.mirror", "{step}");

    public static void RecordDedupHits(string reason, long count = 1) =>
        DedupHits.Add(count, new KeyValuePair<string, object?>("reason", reason));

    public static void RecordChangeEvent(string entity, DbChangeEventType eventType) =>
        ChangeEvents.Add(1,
            new KeyValuePair<string, object?>("entity", entity),
            new KeyValuePair<string, object?>("event_type", eventType.ToString()));

    public static void RecordAccountEvent(string eventName, bool succeeded) =>
        AccountEvents.Add(1,
            new KeyValuePair<string, object?>("event", eventName),
            new KeyValuePair<string, object?>("outcome", succeeded ? "ok" : "failed"));

    public static void RecordContactLink(string operation, bool succeeded) =>
        ContactLinks.Add(1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("outcome", succeeded ? "ok" : "failed"));

    public static void RecordMeContactMirror(string step, string outcome) =>
        MeContactMirror.Add(1,
            new KeyValuePair<string, object?>("step", step),
            new KeyValuePair<string, object?>("outcome", outcome));
}
