using System.Globalization;
using Google.Protobuf.WellKnownTypes;
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
        using var check = mailbox.ListFolders(new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false }, cancellationToken: ct);
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

            // TODO: we need to clean this up a bit
            if (type == EasFolderType.Contacts && name == "Windows Live Contacts")
            {
                req.SourceId = "WL";
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

        var folders = mailbox.ListFolders(new ListFoldersRequest() { UserId = userId, IncludeHidden = true, IncludeDeleted = true }, cancellationToken: ct);
        var contactsFolder = await folders.ResponseStream
            .ReadAllAsync(cancellationToken: ct)
            .FirstOrDefaultAsync(a => a.Type == FolderType.ContactsDefault, cancellationToken: ct)
            ?? throw new InvalidOperationException("Provisioning failed!");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = userId }, cancellationToken: ct);

        var itemsList = mailbox.ListItems(new ListItemsRequest() { UserId = userId, CollectionId = contactsFolder.Id, IncludeDeleted = false }, cancellationToken: ct);
        var meContact = await itemsList.ResponseStream
            .ReadAllAsync(cancellationToken: ct)
                .FirstOrDefaultAsync(a => a.Contact.Email1Address == userInfo.EmailAddress, cancellationToken: ct);

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

        await SeedInboxAsync(userId, userInfo.EmailAddress, ct);

        logger.LogInformation("Provisioned user {User}", userId);
    }

    // Stub ingestion: drop a welcome message into the Inbox on first provision so a fresh device
    // has mail to sync. Real inbound delivery would call MailboxStore.DeliverEmail instead.
    private async Task SeedInboxAsync(string userId, string userEmail, CancellationToken ct)
    {
        var folders = mailbox.ListFolders(new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false }, cancellationToken: ct);
        var inbox = await folders.ResponseStream
            .ReadAllAsync(cancellationToken: ct)
            .FirstOrDefaultAsync(f => f.Type == FolderType.InboxDefault, cancellationToken: ct);
        if (inbox is null) return;

        var existing = mailbox.ListItems(new ListItemsRequest { UserId = userId, CollectionId = inbox.Id, IncludeDeleted = false }, cancellationToken: ct);
        if (await existing.ResponseStream.MoveNext(ct))
            return; // already has mail; don't re-seed

        await mailbox.DeliverEmailAsync(new DeliverEmailRequest
        {
            UserId = userId,
            CollectionId = inbox.Id,
            Email = new EmailItem
            {
                From = "ReLiveWP <noreply@relivewp.net>",
                To = userEmail,
                DisplayTo = userEmail,
                Subject = "Welcome to ReLiveWP",
                ThreadTopic = "Welcome to ReLiveWP",
                MessageClass = "IPM.Note",
                Importance = 1,
                Read = false,
                ContentClass = "urn:content-classes:message",
                DateReceived = Timestamp.FromDateTime(DateTime.UtcNow),
                Body = "Your mailbox is ready. This message was delivered by the ReLiveWP ActiveSync server.",
                BodyType = 1,
                NativeBodyType = 1,
            },
        }, cancellationToken: ct);
    }

    private static FolderType ToProtoFolderType(EasFolderType t)
        => (FolderType)t;
}
