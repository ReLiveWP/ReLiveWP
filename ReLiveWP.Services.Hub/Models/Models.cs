namespace ReLiveWP.Services.Hub.Models;

public record ContactSyncNowModel(string ConnectionId);

public record SetContactSyncModel(string ConnectionId, bool Enabled);

public record ContactSyncModel(
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
    int Skipped);

public record ContactSyncListResponse(IReadOnlyList<ContactSyncModel> Connections);
