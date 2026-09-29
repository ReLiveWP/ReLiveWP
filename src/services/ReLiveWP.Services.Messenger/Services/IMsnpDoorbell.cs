namespace ReLiveWP.Services.Messenger.Services;

public interface IMsnpDoorbell
{
    Task RingIfIdleAsync(string sessionId, CancellationToken ct);
    Task RingDevicesAsync(string userId, CancellationToken ct);
}
