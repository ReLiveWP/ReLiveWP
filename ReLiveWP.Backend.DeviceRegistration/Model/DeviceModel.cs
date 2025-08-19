using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.DeviceRegistration.Model;

[Index(nameof(UniqueId))]
[Index(nameof(CertificateSubject))]
public class DeviceModel
{
    [Key]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The device's own specified Unique ID
    /// </summary>
    public string UniqueId { get; set; } = null!;

    // dunno if i'll need this in this DB, best to be sure
    public string CertificateSubject { get; set; } = null!;
    public string OSVersion { get; set; } = null!;
    public string Locale { get; set; } = null!;

    public string? OwnerId { get; set; }

    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Operator { get; set; }
    public string? IMEI { get; set; }
}
