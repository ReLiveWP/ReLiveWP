namespace ReLiveWP.Services.Messenger.Data;

public record MsnpDevice(string InstanceId, string NotificationUri, DateTimeOffset LastSignIn);

public interface IMsnpDeviceStore
{
    Task RememberDeviceAsync(string userId, MsnpDevice device);
    Task ForgetDeviceAsync(string userId, string instanceId);
    Task<IReadOnlyList<MsnpDevice>> ListDevicesAsync(string userId, DateTimeOffset now);
    Task<bool> TryClaimDoorbellAsync(string userId, string instanceId, TimeSpan window);
}
