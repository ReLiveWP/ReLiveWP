using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

// Backs the EAS FolderSync command. The SyncKey is a watermark into the per-user
// FolderEvent log, letting the server compute deltas against what a device last saw.
public class FolderSyncService
{
    public const int StatusSuccess = 1;
    public const int StatusInvalidSyncKey = 9;

    private const string HierarchyId = SyncState.FolderHierarchyCollectionId;

    private readonly ExchangeDbContext _db;
    private readonly ILogger<FolderSyncService> _logger;

    public FolderSyncService(ExchangeDbContext db, ILogger<FolderSyncService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<FolderSync> SyncAsync(string userId, string deviceId, string? clientSyncKey,
        IReadOnlySet<string>? requestedAnnotations = null, CancellationToken ct = default)
    {
        clientSyncKey ??= "0";

        var state = await _db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == userId && s.DeviceId == deviceId && s.CollectionId == HierarchyId, ct);

        if (clientSyncKey == "0")
            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);

        if (state is null || state.SyncKey != clientSyncKey)
        {
            _logger.LogWarning("FolderSync invalid SyncKey for {User}/{Device}: client={Client} server={Server}",
                userId, deviceId, clientSyncKey, state?.SyncKey ?? "<none>");

            if (state is not null)
            {
                state.SyncKey = "0";
                state.Watermark = 0;
                state.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return await InitialSyncAsync(userId, deviceId, state, requestedAnnotations, ct);
        }

        return await IncrementalSyncAsync(userId, state, requestedAnnotations, ct);
    }

    // True when the client declared at least one of the ABCH folder identity annotations.
    private static bool AbchAnnotationsRequested(IReadOnlySet<string>? requested) =>
        requested is not null &&
        (requested.Contains("SID") || requested.Contains("AN") || requested.Contains("DomainId"));

    private async Task<FolderSync> InitialSyncAsync(string userId, string deviceId, SyncState? state,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        long tip = await _db.FolderEvents.Where(e => e.UserId == userId)
            .MaxAsync(e => (long?)e.Id, ct) ?? 0;

        bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);
        var folders = await _db.Folders
            .Where(f => f.UserId == userId && f.DeletedAt == null &&
                        (!f.IsHidden || abchRequested))
            .ToListAsync(ct);

        if (state is null)
        {
            state = new SyncState { UserId = userId, DeviceId = deviceId, CollectionId = HierarchyId };
            _db.SyncStates.Add(state);
        }
        state.SyncKey = "1";
        state.Watermark = tip;
        state.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var changes = new Changes { Add = folders.Select(f => ToFolderChange(f, requestedAnnotations)).ToList() };
        changes.Count = changes.Add.Count;

        return new FolderSync { Status = StatusSuccess, SyncKey = "1", Changes = changes };
    }

    private async Task<FolderSync> IncrementalSyncAsync(string userId, SyncState state,
        IReadOnlySet<string>? requestedAnnotations, CancellationToken ct)
    {
        var events = await _db.FolderEvents
            .Where(e => e.UserId == userId && e.Id > state.Watermark)
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            state.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return new FolderSync { Status = StatusSuccess, SyncKey = state.SyncKey, Changes = new Changes() };
        }

        var delta = SyncEngine.Collapse(events);

        bool abchRequested = AbchAnnotationsRequested(requestedAnnotations);
        var ids = delta.Added.Concat(delta.Updated).ToList();
        var folders = await _db.Folders
            .Where(f => f.UserId == userId && ids.Contains(f.Id) &&
                        (!f.IsHidden || abchRequested))
            .ToDictionaryAsync(f => f.Id, ct);

        var changes = new Changes
        {
            Add = delta.Added.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations)).ToList(),
            Update = delta.Updated.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id], requestedAnnotations)).ToList(),
            Delete = delta.Deleted.Select(id => new FolderChange { ServerId = id }).ToList(),
        };
        changes.Count = changes.Add.Count + changes.Update.Count + changes.Delete.Count;

        state.SyncKey = SyncEngine.NextSyncKey(state.SyncKey);
        state.Watermark = delta.Watermark;
        state.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new FolderSync { Status = StatusSuccess, SyncKey = state.SyncKey, Changes = changes };
    }

    private static FolderChange ToFolderChange(Folder f, IReadOnlySet<string>? requested) => new()
    {
        ServerId = f.Id,
        ParentId = f.ParentServerId,
        DisplayName = f.DisplayName,
        Type = f.Type,
        Annotations = BuildFolderAnnotations(f, requested),
    };

    // Populate per-folder Live annotations for ABCH/social contact folders.
    // Only adds annotations whose names the client declared in the request.
    private static Annotations? BuildFolderAnnotations(Folder f, IReadOnlySet<string>? requested)
    {
        if (requested is null || requested.Count == 0 || f.SourceId is null)
            return null;

        var items = new List<Annotation>();

        void Add(string name, string? value)
        {
            if (requested.Contains(name) && value is not null)
                items.Add(new Annotation { Name = name, Value = value });
        }

        Add("SID", f.SourceId);
        Add("AN", f.AccountName);

        // Map the short SourceId string to its canonical numeric DomainId.
        // Emit only when the mapping is known so clients don't see a stale/wrong value.
        if (requested.Contains("DomainId") && KnownDomainIds.TryGetValue(f.SourceId, out var domainId))
            items.Add(new Annotation { Name = "DomainId", Value = domainId.ToString() });

        return items.Count > 0 ? new Annotations { Items = items } : null;
    }

    // Subset of ContactDomainProperties relevant to our provisioned folders.
    private static readonly Dictionary<string, int> KnownDomainIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ABCH"]  = 18,
        ["FB"]    = 7,
        ["LI"]    = 8,
        ["GOOG"]  = 20,
        ["YHOO"]  = 21,
        ["TWITR"] = 22,
        ["SKYPE"] = 129,
    };
}
