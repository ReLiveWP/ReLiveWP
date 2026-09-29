using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Data;

public interface IChatStateStore
{
    Task SaveEndpointAsync(ChatEndpoint endpoint);
    Task<ChatEndpoint?> FindEndpointAsync(string endpointId);
    Task<IReadOnlyList<ChatEndpoint>> ListEndpointsAsync(string userId);
    Task RemoveEndpointAsync(ChatEndpoint endpoint);
    Task MarkGoneAsync(ChatEndpoint endpoint, EndpointState reason);
    Task<EndpointState> FindGoneReasonAsync(string endpointId);

    Task TouchEndpointAsync(ChatEndpoint endpoint, DateTimeOffset now, TimeSpan activeWindow);
    Task<IReadOnlyList<string>> ClaimQuietDueAsync(DateTimeOffset now);
    Task<IReadOnlyList<string>> ClaimExpiredAsync(DateTimeOffset now);
    Task ScheduleQuietCheckAsync(string endpointId, DateTimeOffset dueAt);
    Task ScheduleExpiryCheckAsync(string endpointId, DateTimeOffset dueAt);
    Task SchedulePresenceAnnouncementAsync(string userId, DateTimeOffset dueAt);
    Task<IReadOnlyList<string>> ClaimPresenceAnnouncementsAsync(DateTimeOffset now);
    Task<long> CountEndpointsAsync();

    Task<ChatUser?> FindUserAsync(string userId);
    Task<string?> FindUserIdByAddressAsync(string address);
    Task SaveUserAsync(ChatUser user, TimeSpan lifetime);
    Task ForgetUserAsync(ChatUser user);

    Task<SendOutcome?> FindSendOutcomeAsync(string endpointId, string transactionId);
    Task RecordSendOutcomeAsync(string endpointId, string transactionId, SendOutcome outcome, TimeSpan ttl);
    Task<bool> TryTakeTokensAsync(string bucket, double cost, double burst, double refillPerSecond, DateTimeOffset now);

    Task<PresenceAudience?> GetAudienceAsync(string userId);
    Task SetAudienceAsync(string userId, PresenceAudience audience, TimeSpan ttl);
    Task AddAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member);
    Task RemoveAudienceMemberIfCachedAsync(string userId, AudienceSide side, string member);
}
