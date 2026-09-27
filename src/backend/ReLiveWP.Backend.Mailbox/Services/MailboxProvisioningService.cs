using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxProvisioningService(MailboxDbContext db, ILogger<MailboxProvisioningService> logger)
{
    private static readonly (string Name, DbFolderType Type)[] DefaultFolders =
    [
        ("Inbox",         DbFolderType.InboxDefault),
        ("Drafts",        DbFolderType.DraftsDefault),
        ("Deleted Items", DbFolderType.DeletedItemsDefault),
        ("Sent Items",    DbFolderType.SentItemsDefault),
        ("Outbox",        DbFolderType.OutboxDefault),
        ("Tasks",         DbFolderType.TasksDefault),
        ("Calendar",      DbFolderType.CalendarDefault),
        ("Contacts",      DbFolderType.ContactsDefault),
        ("Notes",         DbFolderType.NotesDefault),
        ("Journal",       DbFolderType.JournalDefault),
        ("MeContact",     DbFolderType.MeContact),
    ];

    public async Task ProvisionAsync(string userId, string email, string username, CancellationToken ct = default)
    {
        if (await db.Folders.AnyAsync(f => f.UserId == userId, ct))
        {
            await BackfillExistingAsync(userId, ct);
            return;
        }

        var folders = DefaultFolders.Select(f => new DbFolder
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            DisplayName = f.Name,
            Type = f.Type,
            CreatedAt = DateTime.UtcNow,
            // matches what Exchange's MeContactFolderCreator stamps
            SourceId = f.Type == DbFolderType.MeContact ? "ABCH" : null,
            AccountName = f.Type == DbFolderType.MeContact ? string.Empty : null,
            IsHidden = f.Type == DbFolderType.MeContact,
        }).ToList();

        db.Folders.AddRange(folders);

        // folder ids are already assigned above (client-generated), so looking them up here needs
        // no round trip; one SaveChangesAsync means a failure can never leave a half-provisioned
        // mailbox behind for the AnyAsync guard above to refuse to retry.
        var contactsId = folders.First(f => f.Type == DbFolderType.ContactsDefault).Id;
        var inboxId = folders.First(f => f.Type == DbFolderType.InboxDefault).Id;

        SeedMeContact(userId, contactsId, email, username);
        SeedWelcome(userId, inboxId, email);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("provisioned mailbox for {User}", userId);
    }

    // a mailbox provisioned before folder 27 and the 8.1 annotations existed still needs both. the
    // link resolver tops up ordinary contacts on its own pass, but it skips the me contact.
    private async Task BackfillExistingAsync(string userId, CancellationToken ct)
    {
        var folders = await AddMissingDefaultFoldersAsync(userId, ct);
        var me = await TopUpMeContactAnnotationAsync(userId, ct);

        if (folders || me)
            await db.SaveChangesAsync(ct);
    }

    private async Task<bool> TopUpMeContactAnnotationAsync(string userId, CancellationToken ct)
    {
        var annotation = await db.ContactAnnotations
            .FirstOrDefaultAsync(a => a.ContactItem.UserId == userId
                                   && a.ContactItem.DeletedAt == null
                                   && a.ContactType == "Me", ct);

        if (annotation is null || annotation.SourceId is not null)
            return false;

        annotation.SourceId = "WL";
        annotation.ShellContactType = "Regular";
        annotation.MobileIMEnabled = true;
        logger.LogInformation("topped up the me contact annotation for {User}", userId);
        return true;
    }

    private async Task<bool> AddMissingDefaultFoldersAsync(string userId, CancellationToken ct)
    {
        var have = await db.Folders.Where(f => f.UserId == userId && f.DeletedAt == null)
                                   .Select(f => f.Type)
                                   .ToHashSetAsync(ct);

        var missing = DefaultFolders.Where(f => !have.Contains(f.Type)).ToList();
        if (missing.Count == 0)
            return false;

        foreach (var f in missing)
        {
            db.Folders.Add(new DbFolder
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                DisplayName = f.Name,
                Type = f.Type,
                CreatedAt = DateTime.UtcNow,
                SourceId = f.Type == DbFolderType.MeContact ? "ABCH" : null,
                AccountName = f.Type == DbFolderType.MeContact ? string.Empty : null,
                IsHidden = f.Type == DbFolderType.MeContact,
            });
        }

        logger.LogInformation("added {Count} missing default folder(s) for {User}", missing.Count, userId);
        return true;
    }

    private void SeedMeContact(string userId, string contactsFolderId, string email, string username)
    {
        var id = Guid.NewGuid().ToString("N");
        var mri = "1:" + email;

        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = userId,
            CollectionId = contactsFolderId,
            CreatedAt = DateTime.UtcNow,
            FileAs = username,
            FirstName = username,
            Email1Address = email,
            IMAddress = email,
            IMAddress2 = mri,
        };
        db.Items.Add(contact);

        // no cid on the me contact: the device derives its own, a cid annotation trips the self-cid guard
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = id,
            ContactItem = contact,
            WLId = email,
            ObjectId = userId,
            ImMri = mri,
            ContactType = "Me",
            SourceId = "WL",
            ShellContactType = "Regular",
            MobileIMEnabled = true,
        });
    }

    private void SeedWelcome(string userId, string inboxFolderId, string email)
    {
        var id = Guid.NewGuid().ToString("N");
        db.Items.Add(new DbEmail
        {
            Id = id,
            ServerId = id,
            UserId = userId,
            CollectionId = inboxFolderId,
            CreatedAt = DateTime.UtcNow,
            From = "ReLiveWP <noreply@relivewp.net>",
            To = email,
            DisplayTo = email,
            Subject = "Welcome to ReLiveWP",
            ThreadTopic = "Welcome to ReLiveWP",
            MessageClass = "IPM.Note",
            Importance = 1,
            Read = false,
            ContentClass = "urn:content-classes:message",
            DateReceived = DateTime.UtcNow,
            Body = "It worked! If you're reading this, your device has successfully connected and your mailbox is ready. Have fun!\r\nThis message was delivered by the ReLiveWP System, please do not reply.",
            BodyType = 1,
            NativeBodyType = 1,
        });
    }
}
