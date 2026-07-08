using System.Text;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MimeKit;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

// Handles the outbound ComposeMail commands (SendMail / SmartReply / SmartForward).
// There is no real transport yet: a copy is stored in the user's Sent Items folder when the
// client requests SaveInSentItems, and the original is annotated with the reply/forward verb.
public class OutboundMailService(MailboxStore.MailboxStoreClient mailbox)
{
    public const int VerbReply = 1;
    public const int VerbReplyAll = 2;
    public const int VerbForward = 3;

    // Stores the composed message in Sent Items (when requested). Returns nothing — SendMail's
    // success response is an empty HTTP 200.
    public async Task SendAsync(string userId, string fromAddress, string mime, bool saveInSent, CancellationToken ct)
    {
        if (!saveInSent || string.IsNullOrEmpty(mime))
            return;

        var sentFolderId = await ResolveFolderAsync(userId, FolderType.SentItemsDefault, ct);
        if (sentFolderId is null)
            return;

        var email = ParseMime(mime, fromAddress);
        await mailbox.DeliverEmailAsync(
            new DeliverEmailRequest { UserId = userId, CollectionId = sentFolderId, Email = email },
            cancellationToken: ct);
    }

    // Records that the user replied to / forwarded the original message (MS-ASEMAIL §2.2.2.39).
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

    // Minimal MIME → EmailItem projection. The full RFC822 blob is preserved for ItemOperations
    // Fetch; the parsed fields drive the Sync metadata view of the sent item.
    private static EmailItem ParseMime(string mime, string fromAddress)
    {
        var email = new EmailItem
        {
            MessageClass = "IPM.Note",
            Read = true, // items the user sent are already read
            MimeRaw = mime,
            DateReceived = Timestamp.FromDateTime(DateTime.UtcNow),
        };

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mime));
            var msg = MimeMessage.Load(stream);

            // From is authoritative from the server side per MS-ASCMD §2.2.1.17.
            email.From = string.IsNullOrEmpty(fromAddress) ? msg.From.ToString() : fromAddress;
            if (msg.To.Count > 0) email.To = msg.To.ToString();
            if (msg.Cc.Count > 0) email.Cc = msg.Cc.ToString();
            if (msg.Bcc.Count > 0) email.Bcc = msg.Bcc.ToString();
            if (msg.Subject is not null) email.Subject = msg.Subject;
            if (msg.To.Count > 0) email.DisplayTo = msg.To.ToString();
            if (msg.Date != default) email.DateReceived = Timestamp.FromDateTime(msg.Date.UtcDateTime);

            if (msg.HtmlBody is { } html)
            {
                email.Body = html;
                email.BodyType = 2;
                email.NativeBodyType = 2;
            }
            else if (msg.TextBody is { } text)
            {
                email.Body = text;
                email.BodyType = 1;
                email.NativeBodyType = 1;
            }
        }
        catch
        {
            // Couldn't parse the MIME; keep the raw blob and a sensible From.
            email.From = fromAddress;
        }

        return email;
    }
}
