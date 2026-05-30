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
        ("MeContact",     EasFolderType.MeContact),
    ];

    public async Task EnsureProvisionedAsync(string userId, CancellationToken ct = default)
    {
        // Check if any folder exists for this user.
        using var check = mailbox.ListFolders(new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false });
        if (await check.ResponseStream.MoveNext(ct))
            return;

        var userInfo = await userClient.GetUserInfoAsync(
            new GetUserInfoRequest { UserId = userId.ToUpperInvariant() },
            cancellationToken: ct);

        string? contactsFolderId = null;

        foreach (var (name, type) in DefaultFolders)
        {
            var req = new CreateFolderRequest
            {
                UserId = userId,
                DisplayName = name,
                Type = ToProtoFolderType(type),
            };

            if (type == EasFolderType.MeContact)
            {
                req.SourceId = "ABCH";
                req.IsHidden = true;
            }

            var folder = await mailbox.CreateFolderAsync(req, cancellationToken: ct);

            if (type == EasFolderType.ContactsDefault)
                contactsFolderId = folder.Id;
        }

        // Create Me contact in the Contacts folder.
        if (contactsFolderId is not null)
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
                ContactType = "Me",
            };
            if (userInfo.Puid != 0) ann.Cid = userInfo.Puid;

            await mailbox.CreateItemAsync(new CreateItemRequest
            {
                UserId = userId,
                CollectionId = contactsFolderId,
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
