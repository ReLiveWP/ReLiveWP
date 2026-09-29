using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Data;

public record MsnpGatewayDrain(IReadOnlyList<MsnpCommand> Commands, IReadOnlyList<ChatDelivery> Deliveries)
{
    public static MsnpGatewayDrain Empty { get; } = new([], []);

    public bool IsEmpty => Commands.Count == 0 && Deliveries.Count == 0;
}

public interface IMsnpGatewaySessionStore
{
    Task<MsnpGatewaySession?> FindAsync(string sessionId, CancellationToken ct = default);
    Task TouchAsync(MsnpGatewaySession session, CancellationToken ct = default);
    Task SaveAsync(MsnpGatewaySession session, CancellationToken ct = default);
    Task DeleteAsync(string sessionId, CancellationToken ct = default);
    Task EnqueueAsync(MsnpGatewaySession session, IEnumerable<MsnpCommand> commands, CancellationToken ct = default);
    Task<MsnpGatewayDrain> WaitAndDrainAsync(string sessionId, TimeSpan timeout, CancellationToken ct = default);
    Task RequeueAsync(MsnpGatewaySession session, MsnpGatewayDrain drain);

    Task<long> NotifyAsync(string sessionId);
    Task<bool> HasPendingAsync(string sessionId);
    Task<bool> TryClaimDoorbellAsync(string sessionId, TimeSpan window);
}
