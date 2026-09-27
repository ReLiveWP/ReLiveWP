using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Skybox.Data;

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

    public string? NotificationChannelUrl { get; set; } = null!;

    public SkyDeviceLocation? LastLocation { get; set; }

    // last time the device checked in (from the X-WM-DeviceTime header on any SkyTrigger request).
    public DateTimeOffset? LastSeen { get; set; }
}

[Owned]
public class SkyDeviceLocation
{
    public DateTimeOffset Reported { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public static SkyDeviceLocation Parse(string value)
    {
        var paren = value.IndexOf(')');
        var parts = value[(paren + 2)..].Split(',');
        return new SkyDeviceLocation
        {
            Reported = DateTimeOffset.Parse(value[1..paren], null, DateTimeStyles.RoundtripKind),
            Latitude = double.Parse(parts[0], CultureInfo.InvariantCulture),
            Longitude = double.Parse(parts[1], CultureInfo.InvariantCulture),
            Altitude = double.Parse(parts[2], CultureInfo.InvariantCulture),
        };
    }
}