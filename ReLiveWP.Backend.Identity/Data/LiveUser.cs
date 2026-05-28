using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

public enum LiveUserType
{
    User,
    Device
}

[Index(nameof(Cid))]
[Index(nameof(Puid))]
[Index(nameof(Type))]
public class LiveUser : IdentityUser<Guid>
{
    public string Cid { get; set; } = default!;
    public long Puid { get; set; }
    public LiveUserType Type { get; set; }
}
