using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

// Central mutation service: all Folder and Item writes go through here so that
// the corresponding event-log rows are always created in the same transaction.
public class UserService
{
    private readonly ExchangeDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(ExchangeDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Folder operations ─────────────────────────────────────────────────────

    public async Task<string> AddFolderAsync(string userId, string displayName, FolderType type,
        string parentServerId = "0", CancellationToken ct = default)
    {
        var serverId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Folders.Add(new Folder
        {
            UserId = userId, Id = serverId, ParentServerId = parentServerId,
            DisplayName = displayName, Type = type,
        });

        _db.FolderEvents.Add(new FolderEvent
        {
            UserId = userId, EventType = ChangeEventType.Add, ServerId = serverId,
            ParentServerId = parentServerId, DisplayName = displayName, FolderType = type,
            OccurredAt = now,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return serverId;
    }

    public async Task<bool> UpdateFolderAsync(string userId, string serverId,
        Action<Folder> apply, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var folder = await _db.Folders.SingleOrDefaultAsync(f => f.UserId == userId && f.Id == serverId, ct);
        if (folder is null || folder.DeletedAt is not null) return false;
        apply(folder);
        _db.FolderEvents.Add(new FolderEvent
        {
            UserId = userId, EventType = ChangeEventType.Update, ServerId = serverId,
            ParentServerId = folder.ParentServerId, DisplayName = folder.DisplayName,
            FolderType = folder.Type, OccurredAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> RemoveFolderAsync(string userId, string serverId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var folder = await _db.Folders.SingleOrDefaultAsync(f => f.UserId == userId && f.Id == serverId, ct);
        if (folder is null || folder.DeletedAt is not null) return false;
        folder.DeletedAt = DateTime.UtcNow;
        _db.FolderEvents.Add(new FolderEvent
        {
            UserId = userId, EventType = ChangeEventType.Delete,
            ServerId = serverId, OccurredAt = folder.DeletedAt.Value,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    // ── Item operations ───────────────────────────────────────────────────────

    public async Task<string> AddItemAsync(Item item, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        item.Id = id;
        item.ServerId = id;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Items.Add(item);
        _db.ItemEvents.Add(new ItemEvent
        {
            UserId = item.UserId, CollectionId = item.CollectionId,
            EventType = ChangeEventType.Add, ServerId = id,
            OccurredAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<bool> UpdateItemAsync(string userId, string serverId,
        Action<Item> apply, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var item = await _db.Items.SingleOrDefaultAsync(i => i.UserId == userId && i.ServerId == serverId, ct);
        if (item is null || item.DeletedAt is not null) return false;
        apply(item);
        _db.ItemEvents.Add(new ItemEvent
        {
            UserId = userId, CollectionId = item.CollectionId,
            EventType = ChangeEventType.Update, ServerId = serverId,
            OccurredAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> DeleteItemAsync(string userId, string serverId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var item = await _db.Items.SingleOrDefaultAsync(i => i.UserId == userId && i.ServerId == serverId, ct);
        if (item is null || item.DeletedAt is not null) return false;
        item.DeletedAt = DateTime.UtcNow;
        _db.ItemEvents.Add(new ItemEvent
        {
            UserId = userId, CollectionId = item.CollectionId,
            EventType = ChangeEventType.Delete, ServerId = serverId,
            OccurredAt = item.DeletedAt.Value,
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
