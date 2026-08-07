using System.Security.Cryptography;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MimeKit;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public class OutboundMailService(
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<OutboundMailService> logger)
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
        var email = ParseMime(mimeBytes, fromAddress);

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

    private EmailItem ParseMime(byte[] mimeBytes, string fromAddress)
    {
        var email = new EmailItem
        {
            MessageClass = "IPM.Note",
            Read = true, 
            // Latin1 is a byte-for-byte mapping, so the stored string round-trips the original
            // octets even for 8-bit content that UTF-8 would re-encode
            MimeRaw = Encoding.Latin1.GetString(mimeBytes),
            DateReceived = Timestamp.FromDateTime(DateTime.UtcNow),
        };

        try
        {
            using var stream = new MemoryStream(mimeBytes);
            var msg = MimeMessage.Load(stream);

            // the server-side address is authoritative for From
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

            email.Attachments.AddRange(ExtractAttachments(msg));
        }
        catch(Exception ex)
        {
            // MIME parsing probably failed, keep the raw blob and a sensible From.
            email.From = fromAddress;
            logger.LogWarning(ex, "Failed to parse MIME for outgoing email");
        }

        return email;
    }

    // walks the MIME attachment parts, populating metadata plus the decoded bytes
    // (the bytes ride along on AttachmentItem.content, a write-only field only ever
    // populated on create/deliver requests, never set when reading attachments back)
    private List<AttachmentItem> ExtractAttachments(MimeMessage msg)
    {
        var result = new List<AttachmentItem>();

        foreach (var entity in msg.Attachments)
        {
            if (entity is not MimePart part || part.Content is null)
                continue; // nested message/rfc822 attachments (Method=5) not handled yet

            byte[] bytes;
            try
            {
                using var ms = new MemoryStream();
                part.Content.DecodeTo(ms);
                bytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to decode MIME attachment part {FileName}", part.FileName);
                continue;
            }

            var isInline = string.Equals(part.ContentDisposition?.Disposition, "inline", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(part.ContentId);

            var item = new AttachmentItem
            {
                ContentType = part.ContentType?.MimeType ?? "application/octet-stream",
                EstimatedDataSize = bytes.Length,
                Method = 1, // normal attachment
                IsInline = isInline,
                Content = Google.Protobuf.ByteString.CopyFrom(bytes),
            };

            if (!string.IsNullOrEmpty(part.FileName)) item.DisplayName = part.FileName;
            if (!string.IsNullOrEmpty(part.ContentId)) item.ContentId = part.ContentId;
            if (part.ContentLocation is not null) item.ContentLocation = part.ContentLocation.ToString();

            result.Add(item);
        }

        return result;
    }
}
