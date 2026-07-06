using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId))]
public class LiveRefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = default!;
    public string ServiceTarget { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public string? DeviceId { get; set; }
}
