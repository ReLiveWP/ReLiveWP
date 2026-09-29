using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Services;

public interface IDeliveryQueue
{
    Task<bool> EnqueueAsync(ChatEndpoint target, ChatDelivery delivery);
    Task ClearAsync(string endpointId);
    Task<int> MoveMessagesAsync(string fromEndpointId, ChatEndpoint target);
    Task<IReadOnlyList<string>> ClaimOverflowingAsync(DateTimeOffset now);
    Task ScheduleOverflowEvictionAsync(string endpointId, DateTimeOffset dueAt);
}
