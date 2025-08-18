using Microsoft.AspNetCore.Identity;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveUser : IdentityUser<Guid>
{
    public List<LivePendingOAuth> PendingOAuths { get; set; }
        = new();
    public List<LiveConnectedService> ConnectedServices { get; set; } 
        = new();
}
