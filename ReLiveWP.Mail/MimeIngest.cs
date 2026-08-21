using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using MimeKit;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Mail;

public class MimeIngest(ILogger<MimeIngest> logger)
{
    public EmailItem ToEmailItem(byte[] mimeBytes, string fromAddress)
    {
        var email = new EmailItem
        {
            MessageClass = "IPM.Note",
            MimeRaw = ByteString.CopyFrom(mimeBytes),
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
        catch (Exception ex)
        {
            // MIME parsing probably failed, keep the raw blob and a sensible From.
            email.From = fromAddress;
            logger.LogWarning(ex, "Failed to parse MIME");
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
                Content = ByteString.CopyFrom(bytes),
            };

            if (!string.IsNullOrEmpty(part.FileName)) item.DisplayName = part.FileName;
            if (!string.IsNullOrEmpty(part.ContentId)) item.ContentId = part.ContentId;
            if (part.ContentLocation is not null) item.ContentLocation = part.ContentLocation.ToString();

            result.Add(item);
        }

        return result;
    }
}
