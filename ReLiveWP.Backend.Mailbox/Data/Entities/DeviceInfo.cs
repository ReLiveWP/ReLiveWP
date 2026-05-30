namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public class DbDeviceInfo
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
    public int? EnableOutboundSMS { get; set; }
    public string? MobileOperator { get; set; }

    public DateTime UpdatedAt { get; set; }
}
