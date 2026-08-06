using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Grpc;
using ReLiveWP.Backend.Mailbox.Validation;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Services.Grpc;

public class MailboxStoreService(
    MailboxDbContext db,
    MailboxIntegrityService integrity,
    SyncStateRepairService syncStateReconciler,
    MailboxDeletionService deletion)
    : MailboxStore.MailboxStoreBase
{
    private const int MaxAttachmentContentBytes = 25 * 1024 * 1024;

    private static DbAttachment ToAttachmentEntity(string emailItemId, AttachmentItem a)
    {
        if (a.HasContent && a.Content.Length > MaxAttachmentContentBytes)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"Attachment content exceeds the {MaxAttachmentContentBytes}-byte limit"));
        return MailboxMapper.ToEntity(emailItemId, a);
    }

    public override async Task<Folder> CreateFolder(CreateFolderRequest request, ServerCallContext context)
    {
        var serverId = Guid.NewGuid().ToString("N");
        var entity = MailboxMapper.ToEntity(request, serverId);
        db.Folders.Add(entity);
        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto(entity);
    }

    public override async Task<MutationResult> UpdateFolder(UpdateFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.ServerId,
            context.CancellationToken);

        if (folder is null || folder.DeletedAt is not null)
            return new MutationResult { Found = false };

        folder.DisplayName = request.DisplayName;
        folder.ParentServerId = request.ParentServerId;
        folder.Type = MailboxMapper.FromProto(request.Type);
        folder.SourceId = request.HasSourceId ? request.SourceId : null;
        folder.AccountName = request.HasAccountName ? request.AccountName : null;
        folder.IsHidden = request.IsHidden;

        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = true };
    }

    public override async Task<MutationResult> DeleteFolder(DeleteFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.ServerId,
            context.CancellationToken);

        if (folder is null || folder.DeletedAt is not null)
            return new MutationResult { Found = false };

        var now = DateTime.UtcNow;
        folder.DeletedAt = now;

        // load-and-mark per entity so ChangeLogInterceptor emits per-item events; a bulk update would desync devices
        var items = await db.Items
            .Where(i => i.UserId == request.UserId && i.CollectionId == folder.Id && i.DeletedAt == null)
            .ToListAsync(context.CancellationToken);

        foreach (var item in items)
            item.DeletedAt = now;

        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = true };
    }

    public override async Task<Folder> GetFolder(GetFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.AsNoTracking().SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.ServerId && f.DeletedAt == null,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Folder not found"));

        return MailboxMapper.ToProto(folder);
    }

    public override async Task ListFolders(ListFoldersRequest request, IServerStreamWriter<Folder> stream, ServerCallContext context)
    {
        var query = db.Folders.AsNoTracking().Where(f => f.UserId == request.UserId);
        if (!request.IncludeDeleted) query = query.Where(f => f.DeletedAt == null);
        if (!request.IncludeHidden) query = query.Where(f => !f.IsHidden);

        await foreach (var f in query.AsAsyncEnumerable().WithCancellation(context.CancellationToken))
            await stream.WriteAsync(MailboxMapper.ToProto(f));
    }

    public override async Task<Item> CreateItem(CreateItemRequest request, ServerCallContext context)
    {
        var clientId = request.HasClientId ? request.ClientId : null;
        if (clientId is not null)
        {
            var existing = await FindByClientIdAsync(request.UserId, request.CollectionId, clientId, context.CancellationToken);
            if (existing is not null)
                return MailboxMapper.ToProto(existing);
        }

        var folderExists = await db.Folders.AnyAsync(
            f => f.UserId == request.UserId && f.Id == request.CollectionId && f.DeletedAt == null,
            context.CancellationToken);
        if (!folderExists)
            throw new RpcException(new Status(StatusCode.NotFound, "Folder not found"));

        var id = Guid.NewGuid().ToString("N");
        DbItem entity = request.BodyCase switch
        {
            CreateItemRequest.BodyOneofCase.Contact => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Contact),
            CreateItemRequest.BodyOneofCase.Calendar => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Calendar),
            CreateItemRequest.BodyOneofCase.Task => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Task),
            CreateItemRequest.BodyOneofCase.Email => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Email),
            CreateItemRequest.BodyOneofCase.Note => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Note),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Item body is required")),
        };
        entity.Id = id;
        entity.ServerId = id;
        entity.ClientId = clientId;
        entity.CreatedAt = DateTime.UtcNow;

        db.Items.Add(entity);

        if (request.Annotation != null && entity is DbContactItem contact)
        {
            var ann = MailboxMapper.ToEntity(request.Annotation, id);
            ann.ContactItem = contact;
            db.ContactAnnotations.Add(ann);
        }

        if (entity is DbNote note && request.BodyCase == CreateItemRequest.BodyOneofCase.Note)
            note.Categories = [.. request.Note.Categories.Select(x => new DbNoteCategory
            { Id = Guid.NewGuid().ToString("N"), NoteItemId = note.Id, Category = x.Category })];

        if (entity is DbEmail email && request.BodyCase == CreateItemRequest.BodyOneofCase.Email)
            email.Attachments = [.. request.Email.Attachments.Select(a => ToAttachmentEntity(id, a))];

        try
        {
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (ItemValidationException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
        }
        catch (DbUpdateException) when (clientId is not null)
        {
            // lost the race for this ClientId, look up whoever won
            db.Entry(entity).State = EntityState.Detached;
            var winner = await FindByClientIdAsync(request.UserId, request.CollectionId, clientId, context.CancellationToken);
            if (winner is null) throw;
            return MailboxMapper.ToProto(winner);
        }

        return MailboxMapper.ToProto(entity);
    }

    private Task<DbItem?> FindByClientIdAsync(string userId, string collectionId, string clientId, CancellationToken ct) =>
        db.Items.FirstOrDefaultAsync(
            i => i.UserId == userId && i.CollectionId == collectionId && i.ClientId == clientId && i.DeletedAt == null,
            ct);

    public override async Task<Item> DeliverEmail(DeliverEmailRequest request, ServerCallContext context)
    {
        string collectionId;
        if (request.HasCollectionId && !string.IsNullOrEmpty(request.CollectionId))
        {
            var folderExists = await db.Folders.AnyAsync(
                f => f.UserId == request.UserId && f.Id == request.CollectionId && f.DeletedAt == null,
                context.CancellationToken);
            if (!folderExists)
                throw new RpcException(new Status(StatusCode.NotFound, "Folder not found"));
            collectionId = request.CollectionId;
        }
        else
        {
            collectionId = await db.Folders
                .Where(f => f.UserId == request.UserId && f.DeletedAt == null && f.Type == DbFolderType.InboxDefault)
                .Select(f => f.Id)
                .FirstOrDefaultAsync(context.CancellationToken)
              ?? throw new RpcException(new Status(StatusCode.FailedPrecondition, "No Inbox folder; provision first"));
        }

        var id = Guid.NewGuid().ToString("N");
        var entity = MailboxMapper.ToEntity(request.UserId, collectionId, request.Email);
        entity.Id = id;
        entity.ServerId = id;
        entity.CreatedAt = DateTime.UtcNow;
        if (entity.DateReceived is null)
            entity.DateReceived = DateTime.UtcNow;
        entity.Attachments = [.. request.Email.Attachments.Select(a => ToAttachmentEntity(id, a))];

        db.Items.Add(entity);
        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto((DbItem)entity);
    }

    public override async Task<DeleteMailboxResult> DeleteMailbox(DeleteMailboxRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        var result = await deletion.DeleteAsync(request.UserId, context.CancellationToken);
        return new DeleteMailboxResult
        {
            Found = result.FoldersDeleted > 0 || result.ItemsDeleted > 0,
            FoldersDeleted = result.FoldersDeleted,
            ItemsDeleted = result.ItemsDeleted,
        };
    }

    public override async Task<AttachmentContent> GetAttachmentContent(GetAttachmentContentRequest request, ServerCallContext context)
    {
        var attachment = await db.Attachments
            .Include(a => a.EmailItem)
            .SingleOrDefaultAsync(a => a.Id == request.AttachmentId, context.CancellationToken);

        if (attachment is null || attachment.EmailItem.UserId != request.UserId || attachment.EmailItem.DeletedAt is not null)
            throw new RpcException(new Status(StatusCode.NotFound, "Attachment not found"));

        var p = new AttachmentContent
        {
            Id = attachment.Id,
            Content = Google.Protobuf.ByteString.CopyFrom(attachment.Content ?? []),
        };
        if (attachment.DisplayName is not null) p.DisplayName = attachment.DisplayName;
        if (attachment.ContentType is not null) p.ContentType = attachment.ContentType;
        return p;
    }

    public override async Task<MutationResult> UpdateItem(UpdateItemRequest request, ServerCallContext context)
    {
        var entity = await LoadItemWithChildren(request.UserId, request.ServerId, context.CancellationToken);
        if (entity is null || entity.DeletedAt is not null)
            return new MutationResult { Found = false };

        if (request.HasExpectedVersion && db.Database.IsNpgsql())
            db.Entry(entity).Property(i => i.Version).OriginalValue = request.ExpectedVersion;

        switch (request.BodyCase)
        {
            case UpdateItemRequest.BodyOneofCase.Contact when entity is DbContactItem c:
                MailboxMapper.ApplyToEntity(c, request.Contact);
                await SyncContactChildrenAsync(c, request.Contact, context.CancellationToken);
                if (request.Annotation != null)
                    await UpsertAnnotationAsync(c, request.Annotation, context.CancellationToken);
                break;

            case UpdateItemRequest.BodyOneofCase.Calendar when entity is DbCalendarItem cal:
                MailboxMapper.ApplyToEntity(cal, request.Calendar);
                await SyncCalendarChildrenAsync(cal, request.Calendar, context.CancellationToken);
                break;

            case UpdateItemRequest.BodyOneofCase.Email when entity is DbEmail mail:
                MailboxMapper.ApplyToEntity(mail, request.Email);
                break;

            case UpdateItemRequest.BodyOneofCase.Task when entity is DbTask task:
                MailboxMapper.ApplyToEntity(task, request.Task);
                break;

            case UpdateItemRequest.BodyOneofCase.Note when entity is DbNote note:
                MailboxMapper.ApplyToEntity(note, request.Note);
                await SyncNoteChildrenAsync(note, request.Note, context.CancellationToken);
                break;
        }

        try
        {
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (ItemValidationException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new MutationResult { Found = true, Conflict = true };
        }
        return new MutationResult { Found = true };
    }

    public override async Task<MutationResult> DeleteItem(DeleteItemRequest request, ServerCallContext context)
    {
        var entity = await db.Items.SingleOrDefaultAsync(
            i => i.UserId == request.UserId && i.ServerId == request.ServerId,
            context.CancellationToken);

        if (entity is null || entity.DeletedAt is not null)
            return new MutationResult { Found = false };

        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = true };
    }

    public override async Task<MoveItemResult> MoveItem(MoveItemRequest request, ServerCallContext context)
    {
        var entity = await db.Items.SingleOrDefaultAsync(
            i => i.UserId == request.UserId && i.ServerId == request.ServerId,
            context.CancellationToken);

        if (entity is null || entity.DeletedAt is not null)
            return new MoveItemResult { Status = MoveItemStatus.MoveInvalidSource };

        if (entity.CollectionId == request.DstCollectionId)
            return new MoveItemResult { Status = MoveItemStatus.MoveSameCollection };

        var dstExists = await db.Folders.AnyAsync(
            f => f.UserId == request.UserId && f.Id == request.DstCollectionId && f.DeletedAt == null,
            context.CancellationToken);
        if (!dstExists)
            return new MoveItemResult { Status = MoveItemStatus.MoveInvalidDest };

        entity.CollectionId = request.DstCollectionId;
        try
        {
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (ItemValidationException)
        {
            // attempts to e.g. move a contact into a mail folder
            return new MoveItemResult { Status = MoveItemStatus.MoveInvalidClass };
        }

        return new MoveItemResult { Status = MoveItemStatus.MoveSuccess, NewServerId = entity.ServerId };
    }

    public override async Task<MoveConversationResult> MoveConversation(
        MoveConversationRequest request, ServerCallContext context)
    {
        var conversationId = request.ConversationId.ToByteArray();

        var emails = await db.Items.OfType<DbEmail>()
            .Where(e => e.UserId == request.UserId && e.DeletedAt == null
                     && e.ConversationId != null && e.ConversationId == conversationId)
            .ToListAsync(context.CancellationToken);

        if (emails.Count == 0)
            return new MoveConversationResult { Status = MoveItemStatus.MoveInvalidSource };

        // same-collection is only "same" if every message in the conversation is already there
        if (emails.All(e => e.CollectionId == request.DstCollectionId))
            return new MoveConversationResult { Status = MoveItemStatus.MoveSameCollection };

        var dstExists = await db.Folders.AnyAsync(
            f => f.UserId == request.UserId && f.Id == request.DstCollectionId && f.DeletedAt == null,
            context.CancellationToken);
        if (!dstExists)
            return new MoveConversationResult { Status = MoveItemStatus.MoveInvalidDest };

        // load-and-mark per entity (not a bulk SQL update) so ChangeLogInterceptor emits
        // per-item events; a bulk update would desync devices, matching EmptyFolder below
        foreach (var email in emails)
            email.CollectionId = request.DstCollectionId;

        await db.SaveChangesAsync(context.CancellationToken);

        return new MoveConversationResult
        {
            Status = MoveItemStatus.MoveSuccess,
            ItemsMoved = emails.Count,
        };
    }

    public override async Task<EmptyFolderResult> EmptyFolder(EmptyFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.CollectionId && f.DeletedAt == null,
            context.CancellationToken);
        if (folder is null)
            return new EmptyFolderResult { Found = false };

        var collectionIds = new List<string> { folder.Id };
        if (request.DeleteSubFolders)
        {
            var queue = new Queue<string>();
            queue.Enqueue(folder.Id);
            while (queue.Count > 0)
            {
                var parentId = queue.Dequeue();
                var children = await db.Folders
                    .Where(f => f.UserId == request.UserId && f.ParentServerId == parentId && f.DeletedAt == null)
                    .ToListAsync(context.CancellationToken);
                foreach (var child in children)
                {
                    child.DeletedAt = DateTime.UtcNow;
                    collectionIds.Add(child.Id);
                    queue.Enqueue(child.Id);
                }
            }
        }

        // load-and-mark per entity so ChangeLogInterceptor emits per-item events; a bulk update would desync devices
        var items = await db.Items
            .Where(i => i.UserId == request.UserId && collectionIds.Contains(i.CollectionId) && i.DeletedAt == null)
            .ToListAsync(context.CancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in items)
            item.DeletedAt = now;

        await db.SaveChangesAsync(context.CancellationToken);
        return new EmptyFolderResult { Found = true, ItemsDeleted = items.Count };
    }

    public override async Task<Item> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var entity = await LoadItemWithChildren(request.UserId, request.ServerId, context.CancellationToken, noTracking: true)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        return MailboxMapper.ToProto(entity);
    }

    public override async Task GetItems(
        GetItemsRequest request, IServerStreamWriter<Item> stream, ServerCallContext context)
    {
        var items = await db.Items.AsNoTracking()
            .Where(i => i.UserId == request.UserId && request.ServerIds.Contains(i.ServerId)
                     && i.ValidationFlaggedAt == null)
            .ToListAsync(context.CancellationToken);

        await LoadChildrenAsync(items, context.CancellationToken, noTracking: true);

        foreach (var item in items)
            await stream.WriteAsync(MailboxMapper.ToProto(item));
    }

    public override async Task ListItems(
        ListItemsRequest request, IServerStreamWriter<Item> stream, ServerCallContext context)
    {
        var query = db.Items.AsNoTracking().Where(i => i.UserId == request.UserId && i.CollectionId == request.CollectionId
                                     && i.ValidationFlaggedAt == null);
        if (!request.IncludeDeleted) query = query.Where(i => i.DeletedAt == null);

        var items = await query.ToListAsync(context.CancellationToken);
        await LoadChildrenAsync(items, context.CancellationToken, noTracking: true);

        foreach (var item in items)
            await stream.WriteAsync(MailboxMapper.ToProto(item));
    }

    public override async Task<CountResult> CountLiveItems(CountLiveItemsRequest request, ServerCallContext context)
    {
        var count = await db.Items.CountAsync(
            i => i.UserId == request.UserId && i.CollectionId == request.CollectionId
              && i.DeletedAt == null && i.ValidationFlaggedAt == null,
            context.CancellationToken);
        return new CountResult { Count = count };
    }

    private async Task<long> SafeCommitHorizonAsync(CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return long.MaxValue;

        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"SELECT {ChangeEventCursor.SafeHorizonSql}";

        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var value = await cmd.ExecuteScalarAsync(ct);
            return value is long l ? l : long.MaxValue;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public override async Task GetFolderEvents(
        GetFolderEventsRequest request, IServerStreamWriter<FolderEvent> stream, ServerCallContext context)
    {
        var horizon = await SafeCommitHorizonAsync(context.CancellationToken);

        await foreach (var e in db.FolderEvents.AsNoTracking()
            .Where(e => e.UserId == request.UserId
                     && e.CommitId > request.AfterWatermark && e.CommitId < horizon)
            .OrderBy(e => e.CommitId).ThenBy(e => e.Id)
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(MailboxMapper.ToProto(e));
        }
    }

    public override async Task GetItemEvents(
        GetItemEventsRequest request, IServerStreamWriter<ItemEvent> stream, ServerCallContext context)
    {
        var horizon = await SafeCommitHorizonAsync(context.CancellationToken);

        await foreach (var e in db.ItemEvents.AsNoTracking()
            .Where(e => e.UserId == request.UserId
                     && e.CollectionId == request.CollectionId
                     && e.CommitId > request.AfterWatermark && e.CommitId < horizon)
            .OrderBy(e => e.CommitId).ThenBy(e => e.Id)
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(MailboxMapper.ToProto(e));
        }
    }

    public override async Task<Watermark> GetFolderEventTip(FolderEventTipRequest request, ServerCallContext context)
    {
        // a tip is a watermark, so it has to be on the same scale the reads cursor by
        var horizon = await SafeCommitHorizonAsync(context.CancellationToken);
        var tip = await db.FolderEvents
            .Where(e => e.UserId == request.UserId && e.CommitId < horizon)
            .MaxAsync(e => (long?)e.CommitId, context.CancellationToken) ?? 0;
        return new Watermark { Value = tip };
    }

    public override async Task<Watermark> GetItemEventTip(ItemEventTipRequest request, ServerCallContext context)
    {
        var horizon = await SafeCommitHorizonAsync(context.CancellationToken);
        var tip = await db.ItemEvents
            .Where(e => e.UserId == request.UserId && e.CollectionId == request.CollectionId
                     && e.CommitId < horizon)
            .MaxAsync(e => (long?)e.CommitId, context.CancellationToken) ?? 0;
        return new Watermark { Value = tip };
    }

    public override async Task<SyncState> GetSyncState(GetSyncStateRequest request, ServerCallContext context)
    {
        var state = await db.SyncStates.AsNoTracking().SingleOrDefaultAsync(
            s => s.UserId == request.UserId && s.DeviceId == request.DeviceId
              && s.CollectionId == request.CollectionId,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "SyncState not found"));

        return MailboxMapper.ToProto(state);
    }

    private const int MaxSyncStateWriteAttempts = 3;

    // SyncState is written on every Sync/Ping from every device; a concurrent write for the same
    // (user, device, collection) either loses the xmin race (DbUpdateConcurrencyException, caught
    // by UseXminAsConcurrencyToken on DbSyncState) or the unique-index race on first insert
    // (DbUpdateException). Either way, drop our copy and retry against whatever's there now
    // rather than clobbering it; bounded so a genuinely stuck conflict still surfaces.
    public override async Task<SyncState> UpsertSyncState(UpsertSyncStateRequest request, ServerCallContext context)
    {
        for (var attempt = 1; ; attempt++)
        {
            var state = await db.SyncStates.SingleOrDefaultAsync(
                s => s.UserId == request.UserId && s.DeviceId == request.DeviceId
                  && s.CollectionId == request.CollectionId,
                context.CancellationToken);

            var isNew = state is null;
            if (state is null)
            {
                state = new DbSyncState
                {
                    UserId = request.UserId,
                    DeviceId = request.DeviceId,
                    CollectionId = request.CollectionId,
                };
                db.SyncStates.Add(state);
            }

            state.SyncKey = request.SyncKey;
            state.Watermark = request.Watermark;
            state.LastSeenAt = DateTime.UtcNow;
            state.CachedAnnotationNames = request.HasCachedAnnotationNames ? request.CachedAnnotationNames : null;
            state.SupportedElements = request.HasSupportedElements ? request.SupportedElements : null;
            state.PreviousSyncKey = request.PreviousSyncKey;
            state.PreviousWatermark = request.PreviousWatermark;

            try
            {
                await db.SaveChangesAsync(context.CancellationToken);
                return MailboxMapper.ToProto(state);
            }
            catch (Exception ex) when (attempt < MaxSyncStateWriteAttempts &&
                (ex is DbUpdateConcurrencyException || (isNew && ex is DbUpdateException)))
            {
                db.Entry(state).State = EntityState.Detached;
            }
        }
    }

    public override async Task ListSyncStates(
        ListSyncStatesRequest request, IServerStreamWriter<SyncState> stream, ServerCallContext context)
    {
        await foreach (var s in db.SyncStates.AsNoTracking()
            .Where(s => s.UserId == request.UserId && s.DeviceId == request.DeviceId && s.CollectionId != "0")
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(MailboxMapper.ToProto(s));
        }
    }

    public override async Task<ReconcileDuplicateItemsResult> ReconcileDuplicateItems(
        ReconcileDuplicateItemsRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        var query = db.Items.Where(i => i.DeletedAt == null && i.UserId == request.UserId);
        if (!string.IsNullOrEmpty(request.CollectionId))
            query = query.Where(i => i.CollectionId == request.CollectionId);

        var items = await query.ToListAsync(context.CancellationToken);

        int groups = 0, deleted = 0;
        var now = DateTime.UtcNow;

        foreach (var group in items.GroupBy(i => (i.CollectionId, ContentSignature(i))))
        {
            if (group.Count() < 2) continue;
            groups++;

            var survivors = group
                .OrderByDescending(i => string.IsNullOrEmpty(i.ClientId) ? 0 : 1)
                .ThenBy(i => i.CreatedAt ?? DateTime.MaxValue)
                .ThenBy(i => i.ServerId, StringComparer.Ordinal)
                .ToList();

            foreach (var dup in survivors.Skip(1))
            {
                dup.DeletedAt = now;
                deleted++;
            }
        }

        if (deleted > 0)
            await db.SaveChangesAsync(context.CancellationToken);

        return new ReconcileDuplicateItemsResult { GroupsCollapsed = groups, ItemsDeleted = deleted };
    }

    public override async Task<RevalidateItemsResult> RevalidateItems(
        RevalidateItemsRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        var (scanned, corrected, flagged) = await integrity.SweepAsync(
            request.UserId, request.CollectionId, context.CancellationToken);

        return new RevalidateItemsResult
        {
            ItemsScanned = scanned,
            ItemsCorrected = corrected,
            ItemsFlagged = flagged,
        };
    }

    public override async Task<ReconcileSyncStateResult> ReconcileSyncState(
        ReconcileSyncStateRequest request, ServerCallContext context)
    {
        var states = await syncStateReconciler.SweepAsync(
            request.UserId, context.CancellationToken);

        return new ReconcileSyncStateResult
        {
            SyncStatesDeleted = states,
        };
    }

    public override async Task ListFlaggedItems(
        ListFlaggedItemsRequest request, IServerStreamWriter<FlaggedItem> stream, ServerCallContext context)
    {
        var items = await db.Items.AsNoTracking()
            .Where(i => i.UserId == request.UserId && i.ValidationFlaggedAt != null && i.DeletedAt == null)
            .ToListAsync(context.CancellationToken);

        await LoadChildrenAsync(items, context.CancellationToken, noTracking: true);

        foreach (var item in items)
            await stream.WriteAsync(new FlaggedItem
            {
                Item = MailboxMapper.ToProto(item),
                FlaggedAt = MailboxMapper.ToProtoTs(item.ValidationFlaggedAt),
                Reason = item.ValidationReason ?? string.Empty,
            });
    }

    private static string ContentSignature(DbItem item)
    {
        var p = MailboxMapper.ToProto(item);
        p.Id = string.Empty;
        p.ServerId = string.Empty;
        p.CreatedAt = null;
        p.DeletedAt = null;
        p.ClearClientId();
        return p.ToString();
    }

    public override async Task<DeviceInfo> UpsertDeviceInfo(UpsertDeviceInfoRequest request, ServerCallContext context)
    {
        var info = await db.DeviceInfos.SingleOrDefaultAsync(
            d => d.UserId == request.UserId && d.DeviceId == request.DeviceId,
            context.CancellationToken);

        if (info is null)
        {
            info = new DbDeviceInfo { UserId = request.UserId, DeviceId = request.DeviceId };
            db.DeviceInfos.Add(info);
        }

        info.Model = request.HasModel ? request.Model : null;
        info.IMEI = request.HasImei ? request.Imei : null;
        info.FriendlyName = request.HasFriendlyName ? request.FriendlyName : null;
        info.OS = request.HasOs ? request.Os : null;
        info.OSLanguage = request.HasOsLanguage ? request.OsLanguage : null;
        info.PhoneNumber = request.HasPhoneNumber ? request.PhoneNumber : null;
        info.UserAgent = request.HasUserAgent ? request.UserAgent : null;
        info.EnableOutboundSMS = request.HasEnableOutboundSms ? request.EnableOutboundSms : null;
        info.MobileOperator = request.HasMobileOperator ? request.MobileOperator : null;
        info.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto(info);
    }

    public override async Task<DeviceInfo> GetDeviceInfo(GetDeviceInfoRequest request, ServerCallContext context)
    {
        var info = await db.DeviceInfos.AsNoTracking().SingleOrDefaultAsync(
            d => d.UserId == request.UserId && d.DeviceId == request.DeviceId,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "DeviceInfo not found"));

        return MailboxMapper.ToProto(info);
    }

    public override async Task<MutationResult> UpsertContactIdentity(UpsertContactIdentityRequest request, ServerCallContext context)
    {
        var existing = await db.ContactIdentities.SingleOrDefaultAsync(
            x => x.UserId == request.UserId && x.Provider == request.Provider && x.ExternalId == request.ExternalId,
            context.CancellationToken);

        if (existing is null)
        {
            db.ContactIdentities.Add(new DbContactIdentity
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = request.UserId,
                ContactItemId = request.ContactItemId,
                ContactCid = request.ContactCid,
                Provider = request.Provider,
                ExternalId = request.ExternalId,
            });
        }
        else
        {
            existing.ContactItemId = request.ContactItemId;
            existing.ContactCid = request.ContactCid;
        }

        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = existing is not null };
    }

    public override async Task<MutationResult> DeleteContactIdentity(DeleteContactIdentityRequest request, ServerCallContext context)
    {
        var existing = await db.ContactIdentities.SingleOrDefaultAsync(
            x => x.UserId == request.UserId && x.Provider == request.Provider && x.ExternalId == request.ExternalId,
            context.CancellationToken);

        if (existing is null)
            return new MutationResult { Found = false };

        db.ContactIdentities.Remove(existing);
        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = true };
    }

    public override async Task<ResolveAuthorsToContactsResponse> ResolveAuthorsToContacts(ResolveAuthorsToContactsRequest request, ServerCallContext context)
    {
        var response = new ResolveAuthorsToContactsResponse();
        if (request.ExternalIds.Count == 0)
            return response;

        var ids = request.ExternalIds.ToList();
        var rows = await db.ContactIdentities.AsNoTracking()
            .Where(x => x.UserId == request.UserId && x.Provider == request.Provider && ids.Contains(x.ExternalId))
            .ToListAsync(context.CancellationToken);

        foreach (var row in rows)
            response.ContactCids[row.ExternalId] = row.ContactCid;

        return response;
    }

    public override async Task<ResolveContactIdentitiesResponse> ResolveContactIdentities(ResolveContactIdentitiesRequest request, ServerCallContext context)
    {
        var response = new ResolveContactIdentitiesResponse();
        if (request.Cids.Count == 0)
            return response;

        var cids = request.Cids.ToList();
        var rows = await db.ContactIdentities.AsNoTracking()
            .Where(x => x.UserId == request.UserId && cids.Contains(x.ContactCid))
            .ToListAsync(context.CancellationToken);

        foreach (var row in rows)
            response.Identities.Add(new ContactIdentity
            {
                ContactItemId = row.ContactItemId,
                ContactCid = row.ContactCid,
                Provider = row.Provider,
                ExternalId = row.ExternalId,
            });

        return response;
    }

    public override async Task<GetContactProfilesResponse> GetContactProfiles(GetContactProfilesRequest request, ServerCallContext context)
    {
        var response = new GetContactProfilesResponse();
        if (request.Cids.Count == 0)
            return response;

        var cids = request.Cids.ToList();
        var rows = await db.ContactAnnotations.AsNoTracking()
            .Join(db.Items.OfType<DbContactItem>(),
                ann => ann.ContactItemId,
                contact => contact.Id,
                (ann, contact) => new { ann, contact })
            .Where(x => x.contact.UserId == request.UserId
                && x.contact.DeletedAt == null
                && x.ann.Cid != null
                && cids.Contains(x.ann.Cid!.Value))
            .Select(x => new
            {
                Cid = x.ann.Cid!.Value,
                x.contact.FileAs,
                x.contact.FirstName,
                x.contact.LastName,
                x.ann.UserTileUrl,
            })
            .ToListAsync(context.CancellationToken);

        foreach (var group in rows.GroupBy(r => r.Cid))
        {
            var row = group.First();
            var displayName = !string.IsNullOrWhiteSpace(row.FileAs)
                ? row.FileAs
                : string.Join(' ', new[] { row.FirstName, row.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            var profile = new ContactProfile
            {
                Cid = row.Cid,
                DisplayName = displayName ?? "",
            };

            if (!string.IsNullOrWhiteSpace(row.UserTileUrl))
                profile.AvatarUrl = row.UserTileUrl;

            response.Profiles.Add(profile);
        }

        return response;
    }

    // noTracking is only safe for callers that never mutate + save the loaded graph (GetItem,
    // GetItems, ListItems, ListFlaggedItems); UpdateItem reuses this via LoadItemWithChildren and
    // needs the default tracked load.
    private async Task<DbItem?> LoadItemWithChildren(string userId, string serverId, CancellationToken ct, bool noTracking = false)
    {
        var query = db.Items.Where(i => i.UserId == userId && i.ServerId == serverId);
        if (noTracking) query = query.AsNoTracking();
        var item = await query.SingleOrDefaultAsync(ct);

        if (item is not null)
            await LoadChildrenAsync([item], ct, noTracking);

        return item;
    }

    private static IQueryable<T> Track<T>(IQueryable<T> query, bool noTracking) where T : class =>
        noTracking ? query.AsNoTracking() : query;

    private async Task LoadChildrenAsync(IReadOnlyList<DbItem> items, CancellationToken ct, bool noTracking = false)
    {
        var contactIds = items.OfType<DbContactItem>().Select(c => c.Id).ToList();
        var calendarIds = items.OfType<DbCalendarItem>().Select(c => c.Id).ToList();
        var noteIds = items.OfType<DbNote>().Select(n => n.Id).ToList();
        var emailIds = items.OfType<DbEmail>().Select(e => e.Id).ToList();

        if (contactIds.Count > 0)
        {
            var cats = await Track(db.ContactCategories.Where(x => contactIds.Contains(x.ContactItemId)), noTracking).ToListAsync(ct);
            var children = await Track(db.ContactChildren.Where(x => contactIds.Contains(x.ContactItemId)), noTracking).ToListAsync(ct);
            var anns = await Track(db.ContactAnnotations.Where(x => contactIds.Contains(x.ContactItemId)), noTracking).ToListAsync(ct);
            var catMap = cats.ToLookup(x => x.ContactItemId);
            var childMap = children.ToLookup(x => x.ContactItemId);
            var annMap = anns.ToDictionary(x => x.ContactItemId);
            foreach (var c in items.OfType<DbContactItem>())
            {
                c.Categories = [.. catMap[c.Id]];
                c.Children = [.. childMap[c.Id]];
                if (annMap.TryGetValue(c.Id, out var ann)) c.Annotation = ann;
            }
        }

        if (calendarIds.Count > 0)
        {
            var attendees = await Track(db.CalendarAttendees.Where(x => calendarIds.Contains(x.CalendarItemId)), noTracking).ToListAsync(ct);
            var categories = await Track(db.CalendarCategories.Where(x => calendarIds.Contains(x.CalendarItemId)), noTracking).ToListAsync(ct);
            var exceptions = await Track(db.CalendarExceptions.Where(x => calendarIds.Contains(x.CalendarItemId)), noTracking).ToListAsync(ct);
            var exIds = exceptions.Select(e => e.Id).ToList();
            var exAtt = exIds.Count > 0
                ? await Track(db.CalendarExceptionAttendees.Where(x => exIds.Contains(x.CalendarExceptionId)), noTracking).ToListAsync(ct)
                : [];
            var exCat = exIds.Count > 0
                ? await Track(db.CalendarExceptionCategories.Where(x => exIds.Contains(x.CalendarExceptionId)), noTracking).ToListAsync(ct)
                : [];

            var exAttMap = exAtt.ToLookup(x => x.CalendarExceptionId);
            var exCatMap = exCat.ToLookup(x => x.CalendarExceptionId);
            foreach (var ex in exceptions)
            {
                ex.Attendees = [.. exAttMap[ex.Id]];
                ex.Categories = [.. exCatMap[ex.Id]];
            }

            var attMap = attendees.ToLookup(x => x.CalendarItemId);
            var catMap = categories.ToLookup(x => x.CalendarItemId);
            var exMap = exceptions.ToLookup(x => x.CalendarItemId);
            foreach (var cal in items.OfType<DbCalendarItem>())
            {
                cal.Attendees = [.. attMap[cal.Id]];
                cal.Categories = [.. catMap[cal.Id]];
                cal.Exceptions = [.. exMap[cal.Id]];
            }
        }

        if (noteIds.Count > 0)
        {
            var cats = await Track(db.NoteCategories.Where(x => noteIds.Contains(x.NoteItemId)), noTracking).ToListAsync(ct);
            var catMap = cats.ToLookup(x => x.NoteItemId);
            foreach (var n in items.OfType<DbNote>())
                n.Categories = [.. catMap[n.Id]];
        }

        if (emailIds.Count > 0)
        {
            // Content (the blob) is never read off this path (MailboxMapper.ToProto skips it), so
            // don't fetch it. This is a client-projection, so the results start out detached
            // regardless of noTracking; re-attach as Unchanged when the caller might save the
            // parent (UpdateItem never touches Attachments), otherwise EF would mistake these for
            // brand new rows reachable off a tracked email and try to re-insert them.
            var atts = await db.Attachments.Where(x => emailIds.Contains(x.EmailItemId))
                .Select(x => new DbAttachment
                {
                    Id = x.Id,
                    EmailItemId = x.EmailItemId,
                    DisplayName = x.DisplayName,
                    ContentType = x.ContentType,
                    EstimatedDataSize = x.EstimatedDataSize,
                    Method = x.Method,
                    ContentId = x.ContentId,
                    ContentLocation = x.ContentLocation,
                    IsInline = x.IsInline,
                })
                .ToListAsync(ct);

            if (!noTracking)
                db.Attachments.AttachRange(atts);

            var attMap = atts.ToLookup(x => x.EmailItemId);
            foreach (var e in items.OfType<DbEmail>())
                e.Attachments = [.. attMap[e.Id]];
        }
    }

    private async Task SyncContactChildrenAsync(DbContactItem c, ContactItem proto, CancellationToken ct)
    {
        db.ContactCategories.RemoveRange(await db.ContactCategories.Where(x => x.ContactItemId == c.Id).ToListAsync(ct));
        db.ContactChildren.RemoveRange(await db.ContactChildren.Where(x => x.ContactItemId == c.Id).ToListAsync(ct));

        c.Categories = [.. proto.Categories.Select(x => new DbContactCategory { Id = Guid.NewGuid().ToString("N"), ContactItemId = c.Id, Name = x.Name })];
        c.Children = [.. proto.Children.Select(x => new DbContactChild { Id = Guid.NewGuid().ToString("N"), ContactItemId = c.Id, Name = x.Name })];
    }

    private async Task SyncNoteChildrenAsync(DbNote n, NoteItem proto, CancellationToken ct)
    {
        db.NoteCategories.RemoveRange(await db.NoteCategories.Where(x => x.NoteItemId == n.Id).ToListAsync(ct));
        n.Categories = [.. proto.Categories.Select(x => new DbNoteCategory { Id = Guid.NewGuid().ToString("N"), NoteItemId = n.Id, Category = x.Category })];
    }

    private async Task SyncCalendarChildrenAsync(DbCalendarItem cal, CalendarItem proto, CancellationToken ct)
    {
        db.CalendarAttendees.RemoveRange(await db.CalendarAttendees.Where(x => x.CalendarItemId == cal.Id).ToListAsync(ct));
        db.CalendarCategories.RemoveRange(await db.CalendarCategories.Where(x => x.CalendarItemId == cal.Id).ToListAsync(ct));

        var oldExIds = await db.CalendarExceptions.Where(x => x.CalendarItemId == cal.Id).Select(x => x.Id).ToListAsync(ct);
        if (oldExIds.Count > 0)
        {
            db.CalendarExceptionAttendees.RemoveRange(
                await db.CalendarExceptionAttendees.Where(x => oldExIds.Contains(x.CalendarExceptionId)).ToListAsync(ct));
            db.CalendarExceptionCategories.RemoveRange(
                await db.CalendarExceptionCategories.Where(x => oldExIds.Contains(x.CalendarExceptionId)).ToListAsync(ct));
        }
        db.CalendarExceptions.RemoveRange(await db.CalendarExceptions.Where(x => x.CalendarItemId == cal.Id).ToListAsync(ct));

        cal.Attendees = [.. proto.Attendees.Select(a => new DbCalendarAttendee
        {
            Id = Guid.NewGuid().ToString("N"),
            CalendarItemId = cal.Id,
            Email = a.Email,
            Name = a.Name,
            AttendeeStatus = a.HasAttendeeStatus ? (byte)a.AttendeeStatus : null,
            AttendeeType = a.HasAttendeeType ? (byte)a.AttendeeType : null,
        })];

        cal.Categories = [.. proto.Categories.Select(c => new DbCalendarCategory { Id = Guid.NewGuid().ToString("N"), CalendarItemId = cal.Id, Category = c.Category })];

        cal.Exceptions = [.. proto.Exceptions.Select(ex =>
        {
            var exId = Guid.NewGuid().ToString("N");
            return new DbCalendarException
            {
                Id = exId,
                CalendarItemId = cal.Id,
                Deleted = ex.HasDeleted ? ex.Deleted : null,
                ExceptionStartTime = ex.HasExceptionStartTime ? ex.ExceptionStartTime : null,
                InstanceId = ex.HasInstanceId ? ex.InstanceId : null,
                Subject = ex.HasSubject ? ex.Subject : null,
                StartTime = ex.StartTime?.ToDateTime(),
                EndTime = ex.EndTime?.ToDateTime(),
                Location = ex.HasLocation ? ex.Location : null,
                Sensitivity = ex.HasSensitivity ? (byte)ex.Sensitivity : null,
                BusyStatus = ex.HasBusyStatus ? (byte)ex.BusyStatus : null,
                AllDayEvent = ex.HasAllDayEvent ? ex.AllDayEvent : null,
                Reminder = ex.HasReminder ? ex.Reminder : null,
                DtStamp = ex.DtStamp?.ToDateTime(),
                MeetingStatus = ex.HasMeetingStatus ? (byte)ex.MeetingStatus : null,
                AppointmentReplyTime = ex.AppointmentReplyTime?.ToDateTime(),
                ResponseType = ex.HasResponseType ? ex.ResponseType : null,
                OnlineMeetingConfLink = ex.HasOnlineMeetingConfLink ? ex.OnlineMeetingConfLink : null,
                OnlineMeetingExternalLink = ex.HasOnlineMeetingExternalLink ? ex.OnlineMeetingExternalLink : null,
                Notes = ex.HasNotes ? ex.Notes : null,
                BodyLegacy = ex.HasBodyLegacy ? ex.BodyLegacy : null,
                Attendees = [.. ex.Attendees.Select(a => new DbCalendarExceptionAttendee
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CalendarExceptionId = exId,
                    Email = a.Email,
                    Name = a.Name,
                    AttendeeStatus = a.HasAttendeeStatus ? (byte)a.AttendeeStatus : null,
                    AttendeeType = a.HasAttendeeType ? (byte)a.AttendeeType : null,
                })],
                Categories = [.. ex.Categories.Select(c => new DbCalendarExceptionCategory 
                {
                    Id = Guid.NewGuid().ToString("N"), 
                    CalendarExceptionId = exId, 
                    Category = c.Category 
                })],
            };
        })];
    }

    private async Task UpsertAnnotationAsync(DbContactItem c, ContactAnnotation proto, CancellationToken ct)
    {
        var existing = await db.ContactAnnotations.SingleOrDefaultAsync(a => a.ContactItemId == c.Id, ct);
        if (existing is null)
        {
            db.ContactAnnotations.Add(MailboxMapper.ToEntity(proto, c.Id));
        }
        else
        {
            existing.Cid = proto.HasCid ? proto.Cid : null;
            existing.ObjectId = proto.HasObjectId ? proto.ObjectId : null;
            existing.WLId = proto.HasWlId ? proto.WlId : null;
            existing.ImMri = proto.HasImMri ? proto.ImMri : null;
            existing.ContactType = proto.HasContactType ? proto.ContactType : null;
            existing.UserTileUrl = proto.HasUserTileUrl ? proto.UserTileUrl : null;
            existing.UserTileHash = proto.HasUserTileHash ? proto.UserTileHash : null;
            existing.TrustLevel = proto.HasTrustLevel ? proto.TrustLevel : null;
            existing.FavoriteOrder = proto.HasFavoriteOrder ? proto.FavoriteOrder : null;
        }
    }
}
