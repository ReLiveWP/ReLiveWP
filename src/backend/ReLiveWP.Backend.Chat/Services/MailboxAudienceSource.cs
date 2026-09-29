using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Chat.Services;

public class MailboxAudienceSource(MailboxStore.MailboxStoreClient mailbox) : IAudienceSource
{
    public async Task<PresenceAudience> GetAudienceAsync(string userId, CancellationToken ct)
    {
        var reply = await mailbox.ListPresenceAudienceAsync(
            new ListPresenceAudienceRequest { UserId = userId }, cancellationToken: ct);

        return new PresenceAudience(
            CanSee: reply.VisibleUserIds.ToHashSet(StringComparer.Ordinal),
            SeenBy: reply.WatcherUserIds.ToHashSet(StringComparer.Ordinal));
    }
}
