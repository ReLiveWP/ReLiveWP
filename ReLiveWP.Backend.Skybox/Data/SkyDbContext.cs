using System.ComponentModel.DataAnnotations;
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

        optionsBuilder.UseSqlite("Data Source=sky.db");
    }
}

public class SkyDevice
{
    [Key]
    public string DeviceGuid { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public bool Enabled { get; set; } = false;
    public bool SendLocationEnabled { get; set; } = false;
    public bool MPNSEnabled { get; set; } = false;

    public string? Make { get; set; } = null!;
    public string? Model { get; set; } = null!;
    public string? FriendlyName { get; set; } = null!;
    public string OSVersion { get; set; } = null!;
    public string ClientVersion { get; set; } = null!;
    public uint Capabilities { get; set; } = 0;
    public int LCID { get; set; } = 0;
    public string TZ { get; set; } = null!;
    public int ColorTheme { get; set; } = 0;
    public uint ColorAccent { get; set; } = 0;
    public string? PhoneNumber { get; set; } = null!;
    public string? IMSI { get; set; } = null!;
    public string? MobileOperator { get; set; } = null!;
    public string? CommercializedMobileOperator { get; set; } = null!;
    public string? SimId { get; set; } = null!;
    public int MaxWorkingSet { get; set; } = 0;
    public int BatteryLevel { get; set; } = 0;
    public bool PinLocked { get; set; } = false;
    public bool SimLocked { get; set; } = false;
    public long StorageRemaining { get; set; } = 0;
    public string? ScreenResolution { get; set; } = null!;
}