using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyDriveDbContext : DbContext
{
    protected SkyDriveDbContext()
    {
    }

    public SkyDriveDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<SkyLibrary> Libraries { get; set; }
    public DbSet<SkyPhoto> Photos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite("Data Source=skydrive.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SkyLibrary>()
            .HasIndex(l => new { l.OwnerId, l.Category })
            .IsUnique();

        modelBuilder.Entity<SkyPhoto>()
            .HasIndex(p => new { p.OwnerId, p.Category });
    }
}
