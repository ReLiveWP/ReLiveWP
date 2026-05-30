using System.Globalization;
using Grpc.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using EasFolderType = ReLiveWP.Services.Exchange.Models.FolderType;

namespace ReLiveWP.Services.Exchange.Services;

public class ProvisioningService(
    User.UserClient userClient,
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<ProvisioningService> logger)
{
    private static readonly (string Name, EasFolderType Type)[] DefaultFolders =
    [
        ("Inbox",         EasFolderType.InboxDefault),
        ("Drafts",        EasFolderType.DraftsDefault),
        ("Deleted Items", EasFolderType.DeletedItemsDefault),
        ("Sent Items",    EasFolderType.SentItemsDefault),
        ("Outbox",        EasFolderType.OutboxDefault),
        ("Tasks",         EasFolderType.TasksDefault),
        ("Calendar",      EasFolderType.CalendarDefault),
        ("Contacts",      EasFolderType.ContactsDefault),
        ("Notes",         EasFolderType.NotesDefault),
        ("Journal",       EasFolderType.JournalDefault),
        ("Windows Live Contacts",     EasFolderType.Contacts),
    ];

    private async Task EnsureFoldersAsync(string userId, CancellationToken ct)
    {
        using var check = mailbox.ListFolders(new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false });
        if (await check.ResponseStream.MoveNext(ct))
            return;

        string? contactsFolderId = null;

        foreach (var (name, type) in DefaultFolders)
        {
            var req = new CreateFolderRequest
            {
                UserId = userId,
                DisplayName = name,
                Type = ToProtoFolderType(type),
            };

            if (name == "Windows Live Contacts")
            {
                req.SourceId = "WL";
                req.IsHidden = true;
            }

            var folder = await mailbox.CreateFolderAsync(req, cancellationToken: ct);

            if (contactsFolderId == null && type == EasFolderType.Contacts)
            {
                contactsFolderId = folder.Id;
            }
        }
    }

    public async Task EnsureProvisionedAsync(string userId, CancellationToken ct = default)
    {
        // Check if any folder exists for this user.
        await EnsureFoldersAsync(userId, ct);

        
        var folders = mailbox.ListFolders(new ListFoldersRequest() { UserId = userId, IncludeHidden = true, IncludeDeleted = true });

        var contactsFolder = await folders.ResponseStream.ReadAllAsync().FirstOrDefaultAsync(a => a.Type == FolderType.ContactsDefault)
            ?? throw new InvalidOperationException("Provisioning failed!");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = userId });

        var itemsList = mailbox.ListItems(new ListItemsRequest() { UserId = userId, CollectionId = contactsFolder.Id, IncludeDeleted = false });
        var meContact = await itemsList.ResponseStream.ReadAllAsync()
                .FirstOrDefaultAsync(a => a.Contact.Email1Address == userInfo.EmailAddress);

        // Create Me contact in the Contacts folder.
        if (contactsFolder.Id is not null && meContact == null)
        {
            var contact = new ContactItem
            {
                FileAs = userInfo.Username,
                FirstName = userInfo.Username,
                Email1Address = userInfo.EmailAddress,
            };

            var ann = new ContactAnnotation
            {
                ContactItemId = string.Empty, // filled server-side
                WlId = userInfo.EmailAddress,
                ObjectId = userId,
                ImMri = "WL:" + userInfo.Puid,
                UserTileUrl = "http://wamwoowam.co.uk/static/8835590ae4f581354e14177b48f9d95d.png",
                ContactType = "Me",
            };

            if (userInfo.Puid != 0)
                ann.Cid = long.Parse(userInfo.Cid, NumberStyles.HexNumber);

            await mailbox.CreateItemAsync(new CreateItemRequest
            {
                UserId = userId,
                CollectionId = contactsFolder.Id,
                Contact = contact,
                Annotation = ann,
            }, cancellationToken: ct);
        }

        logger.LogInformation("Provisioned user {User}", userId);
    }

    private static FolderType ToProtoFolderType(EasFolderType t) => t switch
    {
        EasFolderType.InboxDefault => FolderType.InboxDefault,
        EasFolderType.DraftsDefault => FolderType.DraftsDefault,
        EasFolderType.DeletedItemsDefault => FolderType.DeletedItemsDefault,
        EasFolderType.SentItemsDefault => FolderType.SentItemsDefault,
        EasFolderType.OutboxDefault => FolderType.OutboxDefault,
        EasFolderType.TasksDefault => FolderType.TasksDefault,
        EasFolderType.CalendarDefault => FolderType.CalendarDefault,
        EasFolderType.ContactsDefault => FolderType.ContactsDefault,
        EasFolderType.NotesDefault => FolderType.NotesDefault,
        EasFolderType.JournalDefault => FolderType.JournalDefault,
        EasFolderType.MeContact => FolderType.MeContact,
        EasFolderType.Mail => FolderType.Mail,
        EasFolderType.Calendar => FolderType.Calendar,
        EasFolderType.Contacts => FolderType.Contacts,
        EasFolderType.Task => FolderType.Task,
        EasFolderType.Journal => FolderType.Journal,
        EasFolderType.Notes => FolderType.Notes,
        _ => FolderType.Generic,
    };
}
