using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class ConnectionLookup(ConnectedServices.ConnectedServicesClient connectedServices)
{
    private const uint PhotoSyncCapability = 0x10;

    public async Task<List<Connection>> AllAsync(CancellationToken ct = default)
    {
        var connections = new List<Connection>();

        var call = connectedServices.GetConnections(new ConnectionsRequest(), cancellationToken: ct);
        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
            connections.Add(connection);

        return connections;
    }

    public async Task<bool> HasPhotoSyncAsync(CancellationToken ct = default)
    {
        var call = connectedServices.GetConnections(
            new ConnectionsRequest { Capabilities = PhotoSyncCapability }, cancellationToken: ct);

        await foreach (var _ in call.ResponseStream.ReadAllAsync(ct))
            return true;

        return false;
    }
}
