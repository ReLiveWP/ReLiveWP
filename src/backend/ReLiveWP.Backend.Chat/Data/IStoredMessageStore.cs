using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Data;

public interface IStoredMessageStore
{
    Task<bool> TryStoreAsync(string recipientId, string senderId, ChatDelivery delivery);
    Task<int> DeliverStoredAsync(string recipientId, IReadOnlyList<ChatEndpoint> targets);
    Task<int> DropExpiredAsync(DateTimeOffset now);
}
