using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId))]
public class LiveSsoSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }

    public bool Persistent { get; set; }

    public string? UserAgent { get; set; }
    public string? CreatedIp { get; set; }
}
