using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Model;

namespace ReLiveWP.Backend.DeviceRegistration.Data;

public class DevicesDbContext : DbContext
{
    protected DevicesDbContext()
    {
    }

    public DevicesDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<DeviceModel> Devices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseSqlite("Data Source=devices.db");
    }
}
