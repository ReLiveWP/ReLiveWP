using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReLiveWP.Backend.DeviceRegistration.Model;

public class ActivationCodeRedemption
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Serial { get; set; }

    public string DeviceUniqueId { get; set; } = null!;
    public string ActivationCode { get; set; } = null!;

    public DateTimeOffset RedeemedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
