using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

[Index(nameof(Cid))]
[Index(nameof(Puid))]
public class LiveUser : IdentityUser<Guid>
{
    public string Cid { get; set; } = default!;
    public long Puid { get; set; }

    public List<LivePendingOAuth> PendingOAuths { get; set; }
        = new();
    public List<LiveConnectedService> ConnectedServices { get; set; } 
        = new();
}
