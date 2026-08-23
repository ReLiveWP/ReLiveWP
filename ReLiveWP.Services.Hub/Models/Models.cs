using ReLiveWP.Services.Grpc.ClearingHouse;

namespace ReLiveWP.Services.Hub.Models;

public record SyncNowModel(string ConnectionId);

public record SetSyncModel(string ConnectionId, bool Enabled);

public record SyncModel(
    string ConnectionId,
    string ServiceId,
    bool Enabled,
    bool Running,
    bool Queued,
    string? LastSyncedAt,
    string? LastFailure,
    int Created,
    int Updated,
    int Deleted,
    int Skipped)
{
    public static SyncModel From(SyncStatus s) => new(
        s.ConnectionId, s.ServiceId, s.Enabled, s.Running, s.Queued,
        s.HasLastSyncedAt ? s.LastSyncedAt : null,
        s.HasLastFailure ? s.LastFailure : null,
        s.Created, s.Updated, s.Deleted, s.Skipped);
}

public record SyncListResponse(IReadOnlyList<SyncModel> Connections);
