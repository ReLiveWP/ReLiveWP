using Grpc.Core;
using ReLiveWP.ServiceDefaults.Contacts;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public sealed record ResolvedConnection(
    string ServiceId, SyncConnection? Usable, bool IsTransient, uint Capabilities)
{
    public bool SuppliesContacts => (Capabilities & ConnectionConsts.ContactsCapability) != 0;
}

public static class ConnectionLookup
{
    public static async Task<ResolvedConnection?> ResolveAsync(
        ConnectedServices.ConnectedServicesClient connected,
        string userId,
        string connectionId,
        Metadata? headers = null,
        bool includeTransient = false,
        CancellationToken ct = default)
    {
        var request = new ConnectionsRequest { IncludeTransient = includeTransient };

        using var call = headers is null
            ? connected.GetConnections(request, cancellationToken: ct)
            : connected.GetConnections(request, headers, cancellationToken: ct);

        Connection? match = null;

        await foreach (var c in call.ResponseStream.ReadAllAsync(ct))
            if (match is null && c.Id == connectionId)
                match = c;

        if (match is null) return null;

        var usable = (match.Flags & ConnectionConsts.BustedFlag) == 0
            ? new SyncConnection(userId, match.Id, match.HasServiceUrl ? match.ServiceUrl : null)
            : null;

        return new ResolvedConnection(
            match.Service, usable, (match.Flags & ConnectionConsts.TransientFlag) != 0, match.Capabilities);
    }
}
