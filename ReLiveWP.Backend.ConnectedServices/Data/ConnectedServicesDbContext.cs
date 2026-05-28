using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class ConnectedServicesDbContext(DbContextOptions<ConnectedServicesDbContext> options) : DbContext(options)
{
    public DbSet<LiveDPoPKey> DPoPKeys { get; set; }
    public DbSet<LivePendingOAuth> PendingOAuths { get; set; }
    public DbSet<LiveConnectedService> ConnectedServices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LiveConnectedService>()
            .HasOne(u => u.DPoPKey);
    }
}
