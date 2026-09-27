using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

public enum LiveUserType
{
    User,
    Device
}

[Index(nameof(Cid), IsUnique = true)]
[Index(nameof(Puid), IsUnique = true)]
[Index(nameof(Type))]
[Index(nameof(SignupActivationCodeHash), IsUnique = true)]
[Index(nameof(InviteSerial), IsUnique = true)]
public class LiveUser : IdentityUser<Guid>
{
    public string Cid { get; set; } = default!;
    public long Puid { get; set; }
    public LiveUserType Type { get; set; }
    public string? DeviceId { get; set; }
    public string? AlternateEmail { get; set; }
    public string? SignupActivationCodeHash { get; set; }
    public int? InviteSerial { get; set; }
    public ICollection<LiveUserCertificate> Certificates { get; set; } = [];
    public LiveUserProfile? Profile { get; set; } = default!;
}

[Owned]
public class LiveUserCertificate
{
    public Guid UserId { get; set; }

    [Key]
    public string Fingerprint { get; set; } = default!;
}
