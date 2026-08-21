using Grpc.Core;
using ReLiveWP.Mail;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mail.Services;

public interface ISentItemsWriter
{
    Task WriteAsync(string userId, string submissionId, string fromAddress, byte[] message, CancellationToken ct);
}

public class SentItemsWriter(
    MailboxStore.MailboxStoreClient mailbox,
    MimeIngest ingest,
    ILogger<SentItemsWriter> logger) : ISentItemsWriter
{
    public async Task WriteAsync(
        string userId, string submissionId, string fromAddress, byte[] message, CancellationToken ct)
    {
        var folderId = await ResolveSentItemsAsync(userId, ct);
        if (folderId is null)
        {
            logger.LogWarning("No Sent Items folder for {UserId}, skipping the sender copy", userId);
            return;
        }

        var item = ingest.ToEmailItem(message, fromAddress);
        item.Read = true;

        await mailbox.CreateItemAsync(
            new CreateItemRequest
            {
                UserId = userId,
                CollectionId = folderId,
                Email = item,
                ClientId = $"sentmail:{submissionId}",
            },
            cancellationToken: ct);
    }

    private async Task<string?> ResolveSentItemsAsync(string userId, CancellationToken ct)
    {
        using var call = mailbox.ListFolders(
            new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false },
            cancellationToken: ct);

        await foreach (var folder in call.ResponseStream.ReadAllAsync(ct))
            if (folder.Type == FolderType.SentItemsDefault)
                return folder.Id;

        return null;
    }
}
