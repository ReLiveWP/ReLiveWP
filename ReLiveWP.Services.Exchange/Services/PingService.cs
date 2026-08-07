using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public class PingService(
    MailboxStore.MailboxStoreClient mailbox,
    PushMonitor monitor,
    PingRegistry registry)
{
    private const int MinHeartbeat = 60;
    private const int MaxHeartbeat = 3540;
    private const int MaxFolders = 200;

    public async Task<PingResponse> PingAsync(string userId,
                                              string deviceId,
                                              Ping? request,
                                              CancellationToken requestAborted)
    {
        var cached = registry.Get(userId, deviceId);

        int? heartbeat = request?.HeartbeatInterval ?? cached?.HeartbeatInterval;
        var folders = request?.Folders?.Folder is { Count: > 0 } f ? f : cached?.Folders;

        if (heartbeat is null || folders is null || folders.Count == 0)
            return new PingResponse { Status = 3 };

        if (heartbeat < MinHeartbeat)
            return new PingResponse { Status = 5, HeartbeatInterval = MinHeartbeat };
        if (heartbeat > MaxHeartbeat)
            return new PingResponse { Status = 5, HeartbeatInterval = MaxHeartbeat };

        if (folders.Count > MaxFolders)
            return new PingResponse { Status = 6, MaxFolders = MaxFolders };

        registry.Store(userId, deviceId, heartbeat.Value, folders);

        foreach (var folder in folders)
        {
            try
            {
                await mailbox.GetFolderAsync(
                    new GetFolderRequest { UserId = userId, ServerId = folder.Id },
                    cancellationToken: requestAborted);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                return new PingResponse { Status = 7 };
            }
        }

        var monitored = folders.Select(x => x.Id).ToHashSet();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(heartbeat.Value);

        var changed = await monitor.WaitForChangesAsync(userId, deviceId, monitored, deadline, requestAborted);

        if (changed.Count > 0)
        {
            return new PingResponse
            {
                Status = 2,
                Folders = new PingResponseFolders { Folder = changed },
            };
        }

        return new PingResponse { Status = 1 };
    }
}
