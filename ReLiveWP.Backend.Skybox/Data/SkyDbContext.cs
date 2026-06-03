using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Skybox.Data;

public class SkyDbContext : DbContext
{
    protected SkyDbContext()
    {
    }

    public SkyDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<SkyDevice> Devices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite("Data Source=sky.db");
    }
}
