using System.Security.Cryptography;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Mail;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public class OutboundMailService(
    MailboxStore.MailboxStoreClient mailbox,
    MimeIngest ingest)
{
    public const int VerbReply = 1;
    public const int VerbReplyAll = 2;
    public const int VerbForward = 3;

    public async Task SendAsync(string userId, string fromAddress, string mime, bool saveInSent, CancellationToken ct)
    {
        if (!saveInSent || string.IsNullOrEmpty(mime))
            return;

        var sentFolderId = await ResolveFolderAsync(userId, FolderType.SentItemsDefault, ct);
        if (sentFolderId is null)
            return;

        var mimeBytes = DecodeMime(mime);
        var email = ingest.ToEmailItem(mimeBytes, fromAddress);
        email.Read = true;

        // CreateItem's ClientId is store-deduped (unique per user+folder), so a retried SendMail
        // depositing the same MIME doesn't leave a second copy in Sent Items
        var clientId = "sentmail:" + Convert.ToHexString(SHA256.HashData(mimeBytes));
        await mailbox.CreateItemAsync(
            new CreateItemRequest { UserId = userId, CollectionId = sentFolderId, Email = email, ClientId = clientId },
            cancellationToken: ct);
    }

    public async Task MarkSourceVerbAsync(string userId, string? serverId, int verb, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(serverId))
            return;

        Item existing;
        try
        {
            existing = await mailbox.GetItemAsync(
                new GetItemRequest { UserId = userId, ServerId = serverId }, cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return;
        }

        if (existing.BodyCase != Item.BodyOneofCase.Email)
            return;

        var email = existing.Email.Clone();
        email.LastVerbExecuted = verb;
        email.LastVerbExecutionTime = Timestamp.FromDateTime(DateTime.UtcNow);

        await mailbox.UpdateItemAsync(
            new UpdateItemRequest { UserId = userId, ServerId = serverId, Email = email },
            cancellationToken: ct);
    }

    private async Task<string?> ResolveFolderAsync(string userId, FolderType type, CancellationToken ct)
    {
        using var call = mailbox.ListFolders(
            new ListFoldersRequest { UserId = userId, IncludeHidden = true, IncludeDeleted = false },
            cancellationToken: ct);

        await foreach (var f in call.ResponseStream.ReadAllAsync(ct))
            if (f.Type == type)
                return f.Id;

        return null;
    }

    private static byte[] DecodeMime(string wire)
    {
        try
        {
            return Convert.FromBase64String(wire);
        }
        catch (FormatException)
        {
            return Encoding.Latin1.GetBytes(wire);
        }
    }

}
