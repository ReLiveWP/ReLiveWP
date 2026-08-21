namespace ReLiveWP.Backend.Mail.Services;

public class MailDeliveryWorker(
    IServiceProvider services,
    ILogger<MailDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private const int BatchSize = 16;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var handled = 0;
            try
            {
                handled = await DrainAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mail delivery sweep failed");
            }

            if (handled == 0)
                await Task.Delay(IdleDelay, stoppingToken);
        }
    }

    private async Task<int> DrainAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IMailQueue>();
        var agents = scope.ServiceProvider.GetServices<IMailDeliveryAgent>().ToDictionary(a => a.Route);

        var batch = await queue.DequeueAsync(BatchSize, ct);
        foreach (var item in batch)
        {
            if (await TryDeliverAsync(agents, item, ct))
                await queue.CompleteAsync(item, ct);
        }

        return batch.Count;
    }

    private async Task<bool> TryDeliverAsync(
        IReadOnlyDictionary<MailRoute, IMailDeliveryAgent> agents, QueuedMail item, CancellationToken ct)
    {
        var delivered = true;

        foreach (var group in item.Envelope.Recipients.GroupBy(r => r.Route))
        {
            if (!agents.TryGetValue(group.Key, out var agent))
            {
                // submission should have rejected these already; drop rather than spin forever
                logger.LogWarning(
                    "No delivery agent for route {Route} on submission {SubmissionId}",
                    group.Key, item.Envelope.SubmissionId);
                continue;
            }

            var envelope = item.Envelope with { Recipients = [.. group] };
            try
            {
                await agent.DeliverAsync(envelope, item.Message, ct);
            }
            catch (Exception ex)
            {
                delivered = false;
                logger.LogError(
                    ex, "Delivery failed for submission {SubmissionId} via {Route}",
                    item.Envelope.SubmissionId, group.Key);
            }
        }

        return delivered;
    }
}
