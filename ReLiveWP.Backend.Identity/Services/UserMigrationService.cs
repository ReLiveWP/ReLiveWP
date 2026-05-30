using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public class UserMigrationService(IServiceProvider serviceProvider,
                                  ILogger<UserMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<LiveDbContext>();

        await foreach (var user in dbContext.Users)
        {
            if (UserUtils.IsValidPuid(user.Type, (ulong)user.Puid))
                continue;

            var newPuid = UserUtils.GenerateUserIds(user.Type);
            logger.LogWarning("User {Username} had invalid PUID {Puid}, regenerating -> {NewPuid}", user.UserName, user.Puid, newPuid.Puid);

            user.Puid = newPuid.Puid;
        }

        await dbContext.SaveChangesAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
