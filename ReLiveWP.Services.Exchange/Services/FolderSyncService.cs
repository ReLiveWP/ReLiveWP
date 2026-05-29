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
        CancellationToken ct = default)
    {
        clientSyncKey ??= "0";

        var state = await _db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == userId && s.DeviceId == deviceId && s.CollectionId == HierarchyId, ct);

        if (clientSyncKey == "0")
            return await InitialSyncAsync(userId, deviceId, state, ct);

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

            return await InitialSyncAsync(userId, deviceId, state, ct);
        }

        return await IncrementalSyncAsync(userId, state, ct);
    }

    private async Task<FolderSync> InitialSyncAsync(string userId, string deviceId, SyncState? state,
        CancellationToken ct)
    {
        long tip = await _db.FolderEvents.Where(e => e.UserId == userId)
            .MaxAsync(e => (long?)e.Id, ct) ?? 0;

        var folders = await _db.Folders
            .Where(f => f.UserId == userId && f.DeletedAt == null)
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

        var changes = new Changes { Add = folders.Select(ToFolderChange).ToList() };
        changes.Count = changes.Add.Count;

        return new FolderSync { Status = StatusSuccess, SyncKey = "1", Changes = changes };
    }

    private async Task<FolderSync> IncrementalSyncAsync(string userId, SyncState state, CancellationToken ct)
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

        var ids = delta.Added.Concat(delta.Updated).ToList();
        var folders = await _db.Folders
            .Where(f => f.UserId == userId && ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        var changes = new Changes
        {
            Add = delta.Added.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id])).ToList(),
            Update = delta.Updated.Where(folders.ContainsKey).Select(id => ToFolderChange(folders[id])).ToList(),
            Delete = delta.Deleted.Select(id => new FolderChange { ServerId = id }).ToList(),
        };
        changes.Count = changes.Add.Count + changes.Update.Count + changes.Delete.Count;

        state.SyncKey = SyncEngine.NextSyncKey(state.SyncKey);
        state.Watermark = delta.Watermark;
        state.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new FolderSync { Status = StatusSuccess, SyncKey = state.SyncKey, Changes = changes };
    }

    private static FolderChange ToFolderChange(Folder f) => new()
    {
        ServerId = f.Id,
        ParentId = f.ParentServerId,
        DisplayName = f.DisplayName,
        Type = f.Type,
    };
}
