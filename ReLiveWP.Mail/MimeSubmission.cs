using MimeKit;
using MimeKit.Utils;

namespace ReLiveWP.Mail;

// what a submitted message looks like once it has been read: who it is for, who it claims to be
// from, and the exact bytes each recipient should receive.
public sealed record PreparedSubmission(
    string? From,
    IReadOnlyList<string> Recipients,
    byte[] DeliveryBytes,
    byte[] SentItemsBytes);

public static class MimeSubmission
{
    // the sender's own copy keeps Bcc so they can see who they blind-copied; the delivered copy
    // must not carry it or every recipient learns the blind list
    public static bool TryPrepare(byte[] mime, string messageIdDomain, out PreparedSubmission prepared)
    {
        prepared = null!;

        MimeMessage message;
        try
        {
            using var source = new MemoryStream(mime);
            message = MimeMessage.Load(source);
        }
        catch
        {
            return false;
        }

        var recipients = Addresses(message.To)
            .Concat(Addresses(message.Cc))
            .Concat(Addresses(message.Bcc))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (message.MessageId is null)
            message.MessageId = MimeUtils.GenerateMessageId(messageIdDomain);
        if (message.Date == default)
            message.Date = DateTimeOffset.UtcNow;

        var sentItemsBytes = Serialize(message);

        message.Bcc.Clear();
        var deliveryBytes = Serialize(message);

        prepared = new PreparedSubmission(
            message.From.Mailboxes.FirstOrDefault()?.Address,
            recipients,
            deliveryBytes,
            sentItemsBytes);
        return true;
    }

    private static IEnumerable<string> Addresses(InternetAddressList list) =>
        list.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrWhiteSpace(a));

    private static byte[] Serialize(MimeMessage message)
    {
        using var buffer = new MemoryStream();
        message.WriteTo(buffer);
        return buffer.ToArray();
    }
}
