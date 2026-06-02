using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Services.Push.Data;

public class PushDatabase : DbContext
{
    public PushDatabase() { }
    public PushDatabase(DbContextOptions<PushDatabase> options) : base(options) { }

    public DbSet<PushChannel> Channels { get; set; }
    public DbSet<DeviceSession> Sessions { get; set; }
    public DbSet<QueuedNotification> Notifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite("Data Source=push.db");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PushChannel>(e =>
        {
            e.HasKey(c => c.ChannelId);
            e.Property(c => c.ChannelId).ValueGeneratedOnAdd();
            e.HasIndex(c => c.Token).IsUnique();
            e.HasIndex(c => c.DeviceId);
        });

        builder.Entity<DeviceSession>(e =>
        {
            e.HasKey(s => s.Token);
            e.HasIndex(s => s.DeviceId);
        });

        builder.Entity<QueuedNotification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).ValueGeneratedOnAdd();

            e.HasIndex(n => new { n.DeviceId, n.Status });
            e.HasIndex(n => new { n.Status, n.LeaseExpiresAt });
            e.HasIndex(n => n.ExpiresAt);
        });
    }
}
