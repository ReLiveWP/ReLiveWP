using System.Runtime.CompilerServices;
using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Nsp.Messages;
using ReLiveWP.Services.Push.Session;

namespace ReLiveWP.Services.Push.Nsp;

public class NspSession(
    PushSession transport,
    ChannelStore channels,
    NotificationQueue queue,
    PushPresence presence,
    PresenceDirectory directory,
    PushInstance instance,
    IConfiguration configuration,
    ILogger<NspSession> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var transportLoop = transport.RunLoop(ct);
        _ = FlushOnConnectAsync(ct);

        try
        {
            await foreach (var data in transport.Inbound.ReadAllAsync(ct))
                await HandleDataAsync(data, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "NSP session error");
        }
        finally
        {
            var deviceId = transport.Context.DeviceId;
            if (deviceId != null)
            {
                presence.Remove(deviceId, this);
                await directory.RemoveAsync(deviceId, instance.Id);
            }

            await transportLoop;
        }
    }
    
    private const int WindowSize = 16;

    public bool TrySend(uint channelId, NspNotificationClass cls, byte[] payload, string tag = null)
    {
        var package = new NspNotification
        {
            ChannelId = channelId,
            Class = cls,
            Payload = payload,
        }.ToPackage();

        return transport.TrySendData(package.Serialize(), tag);
    }

    private async Task FlushOnConnectAsync(CancellationToken ct)
    {
        try
        {
            await transport.Connected;
        }
        catch (OperationCanceledException)
        {
            return; // session ended before it ever connected
        }

        var deviceId = transport.Context.DeviceId;
        presence.Add(deviceId, this);
        await directory.SetAsync(deviceId, instance.Id);

        try
        {
            await DrainAsync(deviceId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed draining queued notifications for {DeviceId}", deviceId);
        }
    }

    private async Task DrainAsync(string deviceId, CancellationToken ct)
    {
        var pending = new Queue<QueuedNotification>(await queue.ClaimForDeviceAsync(deviceId, ct));
        if (pending.Count == 0)
            return;

        var inflight = new Dictionary<string, QueuedNotification>();

        bool Fill()
        {
            while (inflight.Count < WindowSize && pending.TryDequeue(out var item))
            {
                if (!TrySend((uint)item.ChannelId, (NspNotificationClass)item.NotificationClass, item.Payload, item.StreamId))
                    return false; // socket gone
                inflight[item.StreamId] = item;
            }
            return true;
        }

        if (!Fill())
            return;

        await foreach (var tag in transport.Acknowledged.ReadAllAsync(ct))
        {
            if (inflight.Remove(tag, out var item))
                await queue.CompleteAsync(item, ct);

            if (!Fill() || (inflight.Count == 0 && pending.Count == 0))
                break;
        }
    }

    private async Task HandleDataAsync(PushDataPacket data, CancellationToken ct)
    {
        if (!NspPackage.TryParse(data.Payload, out var nsp))
        {
            logger.LogWarning("Not a valid NSP package ({Len} bytes): {Hex}",
                data.Payload.Length, Convert.ToHexString(data.Payload));
            return;
        }

        logger.LogInformation("NSP binding '{Binding}': {Nsp}", data.Binding ?? "", nsp);

        await foreach (var response in BuildResponses(nsp, ct))
            transport.TrySendData(response.Serialize());
    }

    private async IAsyncEnumerable<NspPackage> BuildResponses(NspPackage package, [EnumeratorCancellation] CancellationToken ct)
    {
        switch (package.Command)
        {
            case NspCommand.CreateNotificationChannel:
                {
                    var request = NspCreateChannelRequest.FromPackage(package);

                    var channel = await channels.RegisterAsync(
                        transport.Context.DeviceId, request.ChannelName, request.ServiceName,
                        request.Version, request.Identifier, ct);
                    var uri = BuildChannelUri(channel.Token);

                    logger.LogInformation("Registered channel '{Name}' (publisher {Publisher}) for {DeviceId} -> {Uri} (id {Id})",
                        request.ChannelName, request.ServiceName, transport.Context.DeviceId, uri, channel.ChannelId);

                    yield return new NspCreateChannelResponse
                    {
                        RequestId = request.RequestId,
                        ChannelId = (uint)channel.ChannelId,
                        ChannelUri = uri,
                    }.ToPackage();
                    break;
                }

            case NspCommand.Configure:
                {
                    var request = NspConfigureRequest.FromPackage(package);

                    logger.LogInformation("Configured channel {Channel} for {DeviceId}",
                        request.ChannelId, transport.Context.DeviceId);

                    yield return new NspConfigureResponse
                    {
                        RequestId = request.RequestId,
                        Options = request.Options,
                    }.ToPackage();
                    break;
                }

            case NspCommand.Deregister:
                {
                    var channelId = package.GetUInt(NspTag.ChannelId) ?? 0;

                    logger.LogInformation("Deregistered channel {Channel} for {DeviceId}",
                        channelId, transport.Context.DeviceId);

                    yield return new NspDeregisterResponse
                    {
                        RequestId = package.RequestId,
                        ChannelId = channelId,
                    }.ToPackage();
                    break;
                }

            case NspCommand.EndpointFlush:
            case NspCommand.MessageConsumed:
                {
                    logger.LogDebug("Acked {Command} for channel {Channel}",
                        package.Command, package.GetUInt(NspTag.ChannelId) ?? 0);

                    yield return new NspEndpointAck
                    {
                        RequestCommand = package.Command,
                        RequestId = package.RequestId,
                        ChannelId = package.GetUInt(NspTag.ChannelId) ?? 0,
                    }.ToPackage();
                    break;
                }

            default:
                logger.LogWarning("No handler for NSP command {Command} (0x{Cmd:X2})",
                    package.Command, (byte)package.Command);
                break;
        }
    }

    private string BuildChannelUri(string token) => string.Format(configuration["Push:ChannelUrlFormat"], token);
}
