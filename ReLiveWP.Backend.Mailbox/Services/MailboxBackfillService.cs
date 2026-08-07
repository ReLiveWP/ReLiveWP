using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxBackfillService(
    IServiceScopeFactory scopeFactory,
    ILogger<MailboxBackfillService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // catches users whose account.created event was missed (service was down, user pre-existed)
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MailboxDbContext>();
            var provisioner = scope.ServiceProvider.GetRequiredService<MailboxProvisioningService>();
            var users = scope.ServiceProvider.GetRequiredService<User.UserClient>();

            var provisioned = await db.Folders.Select(f => f.UserId).Distinct().ToHashSetAsync(stoppingToken);

            var count = 0;
            using var call = users.ListUsers(new ListUsersRequest(), cancellationToken: stoppingToken);
            await foreach (var user in call.ResponseStream.ReadAllAsync(stoppingToken))
            {
                if (provisioned.Contains(user.Id))
                    continue;

                await provisioner.ProvisionAsync(user.Id, user.EmailAddress, user.Username, stoppingToken);
                count++;
            }

            if (count > 0)
                logger.LogInformation("reconciler provisioned {Count} missing mailbox(es)", count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "mailbox reconciliation skipped; will retry next start");
        }
    }
}
