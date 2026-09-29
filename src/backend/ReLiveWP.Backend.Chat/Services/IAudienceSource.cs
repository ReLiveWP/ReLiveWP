using ReLiveWP.Backend.Chat.Data;

namespace ReLiveWP.Backend.Chat.Services;

public interface IAudienceSource
{
    Task<PresenceAudience> GetAudienceAsync(string userId, CancellationToken ct);
}
