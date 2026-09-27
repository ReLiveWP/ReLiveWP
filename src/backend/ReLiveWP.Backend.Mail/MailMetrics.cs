using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Backend.Mail;

public static class MailMetrics
{
    private static readonly Counter<long> Deliveries = ServiceTelemetry.Meter.CreateCounter<long>(
        "relivewp.mail.delivery.outcomes", "{message}");

    public static void RecordDelivery(string outcome) =>
        Deliveries.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
