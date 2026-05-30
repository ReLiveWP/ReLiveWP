using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Exchange.Services;

public class ProvisioningService(
    User.UserClient userClient,
    ExchangeDbContext db,
    ILogger<ProvisioningService> logger)
{
    private static readonly (string Name, FolderType Type)[] DefaultFolders =
    [
        ("Inbox",         FolderType.InboxDefault),
        ("Drafts",        FolderType.DraftsDefault),
        ("Deleted Items", FolderType.DeletedItemsDefault),
        ("Sent Items",    FolderType.SentItemsDefault),
        ("Outbox",        FolderType.OutboxDefault),
        ("Tasks",         FolderType.TasksDefault),
        ("Calendar",      FolderType.CalendarDefault),
        ("Contacts",      FolderType.ContactsDefault),
        ("Notes",         FolderType.NotesDefault),
        ("Journal",       FolderType.JournalDefault),
        ("MeContact",     FolderType.MeContact),
    ];

    public async Task EnsureProvisionedAsync(string userId, CancellationToken ct = default)
    {
        if (await db.Folders.AnyAsync(f => f.UserId == userId, ct))
            return;

        var userInfo = await userClient.GetUserInfoAsync(
            new GetUserInfoRequest { UserId = userId.ToUpperInvariant() },
            cancellationToken: ct);

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        string? meContactFolderId = null;
        foreach (var (name, type) in DefaultFolders)
        {
            var serverId = Guid.NewGuid().ToString("N");
            var folder = new Folder
            {
                UserId = userId,
                Id = serverId,
                ParentServerId = "0",
                DisplayName = name,
                Type = type,
            };

            if (type == FolderType.MeContact)
            {
                // "ABCH" identifies the Address Book Contact Hub — the Live People source.
                // Hidden so it doesn't appear as a user-visible contacts folder.
                folder.SourceId = "ABCH";
                folder.IsHidden = true;
            }

            if (type == FolderType.ContactsDefault)
            {
                meContactFolderId = serverId;
            }

            db.Folders.Add(folder);
            db.FolderEvents.Add(new FolderEvent
            {
                UserId = userId,
                EventType = ChangeEventType.Add,
                ServerId = serverId,
                ParentServerId = "0",
                DisplayName = name,
                FolderType = type,
                OccurredAt = now,
            });
        }

        // Create the "Me" contact in the MeContact folder.
        // This mirrors Exchange's ConsumerMeContactSyncCollection: the MeContact
        // folder holds exactly one IPM.Contact representing the signed-in user.
        if (meContactFolderId is not null)
        {
            var contactId = Guid.NewGuid().ToString("N");

            var contact = new ContactItem
            {
                UserId = userId,
                CollectionId = meContactFolderId,
                Id = contactId,
                ServerId = contactId,
                FileAs = userInfo.Username,
                FirstName = userInfo.Username,
                Email1Address = userInfo.EmailAddress,
            };

            // Populate Live annotations from the user's own identity.
            // Puid → CID: the user's own 64-bit Windows Live identifier.
            // WLID    : the user's Live ID (email address).
            // ObjectId: use userId as a stable opaque object identifier.
            // Type    : "Regular" is the standard contact type.
            contact.Annotation = new ContactAnnotation
            {
                ContactItemId = contactId,
                Cid = userInfo.Puid,
                ObjectId = userId,
                WLId = userInfo.EmailAddress,
                ImMri = "WL:" + userInfo.Puid,
                ContactType = "Me",
            };

            db.Items.Add(contact);
            db.ItemEvents.Add(new ItemEvent
            {
                UserId = userId,
                CollectionId = meContactFolderId,
                EventType = ChangeEventType.Add,
                ServerId = contactId,
                OccurredAt = now,
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Provisioned user {User}", userId);
    }
}
