namespace ReLiveWP.Services.Hub.Models;

public record ContactSourceModel(string Id, string DisplayName, int? Count, bool IsDefault);

public record ContactSourcesResponse(string ServiceId, IReadOnlyList<ContactSourceModel> Sources);

public record ImportContactsModel(string ConnectionId, string[]? SourceIds = null, bool KeepInSync = false);

public record ImportContactsResponse(IReadOnlyList<string> QueuedSourceIds);

public record SourceSyncStatusModel(
    string ConnectionId,
    string ServiceId,
    string SourceId,
    bool DetachAfterRun,
    bool Running,
    bool Queued,
    string? LastSyncedAt,
    string? LastFailure,
    int ConsecutiveFailures,
    int Created,
    int Updated,
    int Deleted,
    int Skipped);

public record SyncStatusResponse(IReadOnlyList<SourceSyncStatusModel> Sources);
