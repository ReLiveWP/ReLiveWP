using Grpc.Core;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// one FolderType.Calendar folder per remote collection. Unlike contacts, which merge into the
// undeletable default folder, a type 13 folder can be renamed and deleted on the phone, so this
// has to cope with the user having done either.
public class CalendarFolderResolver(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<CalendarFolderResolver> logger)
{
    private const string RootParentId = "0";

    public async ValueTask<string> ResolveAsync(DbSyncSource source, CancellationToken ct = default)
    {
        if (source.FolderId is { Length: > 0 } existing)
        {
            if (await LiveAsync(source.UserId, existing, ct))
            {
                await RenameIfRemoteChangedAsync(source, existing, ct);
                return existing;
            }

            // deleting the folder is how you say you no longer want this calendar. putting it back
            // would resurrect it on every poll, forever.
            source.SyncEnabled = false;
            source.FolderId = null;

            logger.LogInformation("the folder for {Service}/{Source} was deleted on the device; turning that calendar off",
                source.ServiceId, source.SourceId);

            throw new MirrorException("the calendar folder was deleted on the device");
        }

        return await CreateAsync(source, ct);
    }

    private async Task<string> CreateAsync(DbSyncSource source, CancellationToken ct)
    {
        var name = DisplayName(source);

        var folder = await mailbox.CreateFolderAsync(new CreateFolderRequest
        {
            UserId = source.UserId,
            DisplayName = name,
            Type = FolderType.Calendar,
            ParentServerId = RootParentId,
        }, cancellationToken: ct);

        source.FolderId = folder.Id;
        source.FolderDisplayName = name;

        logger.LogInformation("created calendar folder {Folder} ({Name}) for {Service}/{Source}",
            folder.Id, name, source.ServiceId, source.SourceId);

        return folder.Id;
    }

    // only a change on the remote side pushes a rename. a rename made on the phone is left alone,
    // because the comparison never looks at what the folder is currently called.
    private async Task RenameIfRemoteChangedAsync(DbSyncSource source, string folderId, CancellationToken ct)
    {
        var name = DisplayName(source);
        if (name == source.FolderDisplayName) return;

        var folder = await mailbox.GetFolderAsync(
            new GetFolderRequest { UserId = source.UserId, ServerId = folderId }, cancellationToken: ct);

        await mailbox.UpdateFolderAsync(new UpdateFolderRequest
        {
            UserId = source.UserId,
            ServerId = folderId,
            DisplayName = name,
            ParentServerId = folder.ParentServerId,
            Type = folder.Type,
        }, cancellationToken: ct);

        source.FolderDisplayName = name;

        logger.LogInformation("renamed calendar folder {Folder} to {Name} for {Service}/{Source}",
            folderId, name, source.ServiceId, source.SourceId);
    }

    private async Task<bool> LiveAsync(string userId, string folderId, CancellationToken ct)
    {
        try
        {
            await mailbox.GetFolderAsync(
                new GetFolderRequest { UserId = userId, ServerId = folderId }, cancellationToken: ct);

            return true;
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    private static string DisplayName(DbSyncSource source) =>
        source.RemoteDisplayName is { Length: > 0 } name ? name : source.SourceId;
}
