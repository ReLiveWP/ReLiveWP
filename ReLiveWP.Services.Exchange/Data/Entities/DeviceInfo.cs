namespace ReLiveWP.Services.Exchange.Data.Entities;

// Stores device metadata reported via the Settings command DeviceInformation/Set.
// Keyed by (UserId, DeviceId); upserted on each Settings request.
public class DeviceInfo
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;

    public string? Model { get; set; }
    public string? IMEI { get; set; }
    public string? FriendlyName { get; set; }
    public string? OS { get; set; }
    public string? OSLanguage { get; set; }
    public string? PhoneNumber { get; set; }
    public string? UserAgent { get; set; }
    public int? EnableOutboundSMS { get; set; }   // 0/1
    public string? MobileOperator { get; set; }

    public DateTime UpdatedAt { get; set; }
}
