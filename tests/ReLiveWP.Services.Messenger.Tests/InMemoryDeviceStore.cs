using ReLiveWP.Services.Messenger.Data;

namespace ReLiveWP.Services.Messenger.Tests;

internal sealed class InMemoryDeviceStore : IMsnpDeviceStore
{
    private readonly HashSet<(string UserId, string InstanceId)> claimed = [];

    public Dictionary<string, Dictionary<string, MsnpDevice>> Devices { get; } = [];

    public IReadOnlyCollection<MsnpDevice> DevicesOf(string userId) =>
        Devices.TryGetValue(userId, out var devices) ? devices.Values : [];

    public Task RememberDeviceAsync(string userId, MsnpDevice device)
    {
        if (!Devices.TryGetValue(userId, out var devices))
            Devices[userId] = devices = new Dictionary<string, MsnpDevice>(StringComparer.OrdinalIgnoreCase);

        devices[device.InstanceId] = device;
        return Task.CompletedTask;
    }

    public Task ForgetDeviceAsync(string userId, string instanceId)
    {
        if (Devices.TryGetValue(userId, out var devices))
            devices.Remove(instanceId);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MsnpDevice>> ListDevicesAsync(string userId, DateTimeOffset now) =>
        Task.FromResult<IReadOnlyList<MsnpDevice>>([.. DevicesOf(userId)]);

    public Task<bool> TryClaimDoorbellAsync(string userId, string instanceId, TimeSpan window) =>
        Task.FromResult(claimed.Add((userId, instanceId.ToLowerInvariant())));
}
