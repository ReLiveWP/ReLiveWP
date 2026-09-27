namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxRepairService(
    IServiceScopeFactory scopeFactory,
    ILogger<MailboxRepairService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // rarely does anything; catches items that violate rules added after they were written
            using var scope = scopeFactory.CreateScope();
            var integrity = scope.ServiceProvider.GetRequiredService<MailboxIntegrityService>();

            var (scanned, corrected, flagged) = await integrity.SweepAsync(null, null, stoppingToken);

            if (corrected > 0 || flagged > 0)
                logger.LogInformation(
                    "integrity sweep: scanned {Scanned}, corrected {Corrected}, flagged {Flagged}",
                    scanned, corrected, flagged);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "mailbox integrity sweep skipped; will retry next start");
        }
    }
}
