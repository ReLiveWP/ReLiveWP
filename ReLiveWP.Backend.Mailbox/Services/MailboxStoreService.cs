using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxStoreService(MailboxDbContext db) : MailboxStore.MailboxStoreBase
{
    // ── Folders ───────────────────────────────────────────────────────────────
    public override async Task<Folder> CreateFolder(
        CreateFolderRequest request, ServerCallContext context)
    {
        var serverId = Guid.NewGuid().ToString("N");
        var entity = MailboxMapper.ToEntity(request, serverId);
        db.Folders.Add(entity);
        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto(entity);
    }

    public override async Task<MutationResult> UpdateFolder(
        UpdateFolderRequest request, ServerCallContext context)
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

    public override async Task<MutationResult> DeleteFolder(
        DeleteFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.ServerId,
            context.CancellationToken);

        if (folder is null || folder.DeletedAt is not null)
            return new MutationResult { Found = false };

        folder.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);
        return new MutationResult { Found = true };
    }

    public override async Task<Folder> GetFolder(
        GetFolderRequest request, ServerCallContext context)
    {
        var folder = await db.Folders.SingleOrDefaultAsync(
            f => f.UserId == request.UserId && f.Id == request.ServerId,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Folder not found"));

        return MailboxMapper.ToProto(folder);
    }

    public override async Task ListFolders(
        ListFoldersRequest request,
        IServerStreamWriter<Folder> stream,
        ServerCallContext context)
    {
        var query = db.Folders.Where(f => f.UserId == request.UserId);
        if (!request.IncludeDeleted) query = query.Where(f => f.DeletedAt == null);
        if (!request.IncludeHidden) query = query.Where(f => !f.IsHidden);

        await foreach (var f in query.AsAsyncEnumerable().WithCancellation(context.CancellationToken))
            await stream.WriteAsync(MailboxMapper.ToProto(f));
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    public override async Task<Item> CreateItem(CreateItemRequest request, ServerCallContext context)
    {
        var id = Guid.NewGuid().ToString("N");
        DbItem entity = request.BodyCase switch
        {
            CreateItemRequest.BodyOneofCase.Contact => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Contact),
            CreateItemRequest.BodyOneofCase.Calendar => MailboxMapper.ToEntity(request.UserId, request.CollectionId, request.Calendar),
            CreateItemRequest.BodyOneofCase.Task => new DbTask { UserId = request.UserId, CollectionId = request.CollectionId },
            CreateItemRequest.BodyOneofCase.Email => new DbEmail { UserId = request.UserId, CollectionId = request.CollectionId },
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Item body is required")),
        };
        entity.Id = id;
        entity.ServerId = id;
        entity.CreatedAt = DateTime.UtcNow;

        db.Items.Add(entity);

        if (request.Annotation != null && entity is DbContactItem contact)
        {
            var ann = MailboxMapper.ToEntity(request.Annotation, id);
            ann.ContactItem = contact;
            db.ContactAnnotations.Add(ann);
        }

        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto(entity);
    }

    public override async Task<MutationResult> UpdateItem(UpdateItemRequest request, ServerCallContext context)
    {
        var entity = await LoadItemWithChildren(request.UserId, request.ServerId, context.CancellationToken);
        if (entity is null || entity.DeletedAt is not null)
            return new MutationResult { Found = false };

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
        }

        await db.SaveChangesAsync(context.CancellationToken);
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

    public override async Task<Item> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var entity = await LoadItemWithChildren(request.UserId, request.ServerId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        return MailboxMapper.ToProto(entity);
    }

    public override async Task GetItems(
        GetItemsRequest request, IServerStreamWriter<Item> stream, ServerCallContext context)
    {
        var items = await db.Items
            .Where(i => i.UserId == request.UserId && request.ServerIds.Contains(i.ServerId))
            .ToListAsync(context.CancellationToken);

        await LoadChildrenAsync(items, context.CancellationToken);

        foreach (var item in items)
            await stream.WriteAsync(MailboxMapper.ToProto(item));
    }

    public override async Task ListItems(
        ListItemsRequest request, IServerStreamWriter<Item> stream, ServerCallContext context)
    {
        var query = db.Items.Where(i => i.UserId == request.UserId && i.CollectionId == request.CollectionId);
        if (!request.IncludeDeleted) query = query.Where(i => i.DeletedAt == null);

        var items = await query.ToListAsync(context.CancellationToken);
        await LoadChildrenAsync(items, context.CancellationToken);

        foreach (var item in items)
            await stream.WriteAsync(MailboxMapper.ToProto(item));
    }

    public override async Task<CountResult> CountLiveItems(
        CountLiveItemsRequest request, ServerCallContext context)
    {
        var count = await db.Items.CountAsync(
            i => i.UserId == request.UserId && i.CollectionId == request.CollectionId && i.DeletedAt == null,
            context.CancellationToken);
        return new CountResult { Count = count };
    }

    // ── Change log ────────────────────────────────────────────────────────────

    public override async Task GetFolderEvents(
        GetFolderEventsRequest request, IServerStreamWriter<FolderEvent> stream, ServerCallContext context)
    {
        await foreach (var e in db.FolderEvents
            .Where(e => e.UserId == request.UserId && e.Id > request.AfterWatermark)
            .OrderBy(e => e.Id)
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(MailboxMapper.ToProto(e));
        }
    }

    public override async Task GetItemEvents(
        GetItemEventsRequest request, IServerStreamWriter<ItemEvent> stream, ServerCallContext context)
    {
        await foreach (var e in db.ItemEvents
            .Where(e => e.UserId == request.UserId
                     && e.CollectionId == request.CollectionId
                     && e.Id > request.AfterWatermark)
            .OrderBy(e => e.Id)
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(MailboxMapper.ToProto(e));
        }
    }

    public override async Task<Watermark> GetFolderEventTip(
        FolderEventTipRequest request, ServerCallContext context)
    {
        var tip = await db.FolderEvents
            .Where(e => e.UserId == request.UserId)
            .MaxAsync(e => (long?)e.Id, context.CancellationToken) ?? 0;
        return new Watermark { Value = tip };
    }

    public override async Task<Watermark> GetItemEventTip(
        ItemEventTipRequest request, ServerCallContext context)
    {
        var tip = await db.ItemEvents
            .Where(e => e.UserId == request.UserId && e.CollectionId == request.CollectionId)
            .MaxAsync(e => (long?)e.Id, context.CancellationToken) ?? 0;
        return new Watermark { Value = tip };
    }

    // ── Sync state ────────────────────────────────────────────────────────────

    public override async Task<SyncState> GetSyncState(
        GetSyncStateRequest request, ServerCallContext context)
    {
        var state = await db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == request.UserId && s.DeviceId == request.DeviceId
              && s.CollectionId == request.CollectionId,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "SyncState not found"));

        return MailboxMapper.ToProto(state);
    }

    public override async Task<SyncState> UpsertSyncState(
        UpsertSyncStateRequest request, ServerCallContext context)
    {
        var state = await db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == request.UserId && s.DeviceId == request.DeviceId
              && s.CollectionId == request.CollectionId,
            context.CancellationToken);

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

        await db.SaveChangesAsync(context.CancellationToken);
        return MailboxMapper.ToProto(state);
    }

    // ── Device info ───────────────────────────────────────────────────────────

    public override async Task<DeviceInfo> UpsertDeviceInfo(
        UpsertDeviceInfoRequest request, ServerCallContext context)
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

    public override async Task<DeviceInfo> GetDeviceInfo(
        GetDeviceInfoRequest request, ServerCallContext context)
    {
        var info = await db.DeviceInfos.SingleOrDefaultAsync(
            d => d.UserId == request.UserId && d.DeviceId == request.DeviceId,
            context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "DeviceInfo not found"));

        return MailboxMapper.ToProto(info);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<DbItem?> LoadItemWithChildren(string userId, string serverId, CancellationToken ct)
    {
        var item = await db.Items
            .SingleOrDefaultAsync(i => i.UserId == userId && i.ServerId == serverId, ct);

        if (item is not null)
            await LoadChildrenAsync([item], ct);

        return item;
    }

    private async Task LoadChildrenAsync(IReadOnlyList<DbItem> items, CancellationToken ct)
    {
        var contactIds = items.OfType<DbContactItem>().Select(c => c.Id).ToList();
        var calendarIds = items.OfType<DbCalendarItem>().Select(c => c.Id).ToList();

        if (contactIds.Count > 0)
        {
            var cats = await db.ContactCategories.Where(x => contactIds.Contains(x.ContactItemId)).ToListAsync(ct);
            var children = await db.ContactChildren.Where(x => contactIds.Contains(x.ContactItemId)).ToListAsync(ct);
            var anns = await db.ContactAnnotations.Where(x => contactIds.Contains(x.ContactItemId)).ToListAsync(ct);
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
            var attendees = await db.CalendarAttendees.Where(x => calendarIds.Contains(x.CalendarItemId)).ToListAsync(ct);
            var categories = await db.CalendarCategories.Where(x => calendarIds.Contains(x.CalendarItemId)).ToListAsync(ct);
            var exceptions = await db.CalendarExceptions.Where(x => calendarIds.Contains(x.CalendarItemId)).ToListAsync(ct);
            var exIds = exceptions.Select(e => e.Id).ToList();
            var exAtt = exIds.Count > 0
                ? await db.CalendarExceptionAttendees.Where(x => exIds.Contains(x.CalendarExceptionId)).ToListAsync(ct)
                : [];
            var exCat = exIds.Count > 0
                ? await db.CalendarExceptionCategories.Where(x => exIds.Contains(x.CalendarExceptionId)).ToListAsync(ct)
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
    }

    private async Task SyncContactChildrenAsync(DbContactItem c, ContactItem proto, CancellationToken ct)
    {
        db.ContactCategories.RemoveRange(await db.ContactCategories.Where(x => x.ContactItemId == c.Id).ToListAsync(ct));
        db.ContactChildren.RemoveRange(await db.ContactChildren.Where(x => x.ContactItemId == c.Id).ToListAsync(ct));

        c.Categories = [.. proto.Categories.Select(x => new DbContactCategory { Id = Guid.NewGuid().ToString("N"), ContactItemId = c.Id, Name = x.Name })];
        c.Children = [.. proto.Children.Select(x => new DbContactChild { Id = Guid.NewGuid().ToString("N"), ContactItemId = c.Id, Name = x.Name })];
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
                StartTime = ex.StartTime != null ? ex.StartTime.ToDateTime() : null,
                EndTime = ex.EndTime != null ? ex.EndTime.ToDateTime() : null,
                Location = ex.HasLocation ? ex.Location : null,
                Sensitivity = ex.HasSensitivity ? (byte)ex.Sensitivity : null,
                BusyStatus = ex.HasBusyStatus ? (byte)ex.BusyStatus : null,
                AllDayEvent = ex.HasAllDayEvent ? ex.AllDayEvent : null,
                Reminder = ex.HasReminder ? ex.Reminder : null,
                DtStamp = ex.DtStamp != null ? ex.DtStamp.ToDateTime() : null,
                MeetingStatus = ex.HasMeetingStatus ? (byte)ex.MeetingStatus : null,
                AppointmentReplyTime = ex.AppointmentReplyTime != null ? ex.AppointmentReplyTime.ToDateTime() : null,
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
                { Id = Guid.NewGuid().ToString("N"), CalendarExceptionId = exId, Category = c.Category })],
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
