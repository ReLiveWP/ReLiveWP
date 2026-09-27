using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mail;
using ReLiveWP.Services.Grpc.Mailbox;
using MailClient = ReLiveWP.Services.Grpc.Mail.Mail.MailClient;

namespace ReLiveWP.Services.Exchange.Services;

public class OutboundMailService(
    MailClient mail,
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<OutboundMailService> logger)
{
    public const int VerbReply = 1;
    public const int VerbReplyAll = 2;
    public const int VerbForward = 3;

    public async Task<int> SubmitAsync(
        string userId, string? mime, bool saveInSent, string? clientId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(mime))
            return EasStatus.MailSubmissionFailed;

        var mimeBytes = DecodeMime(mime);

        var request = new SubmitRequest
        {
            UserId = userId,
            Mime = ByteString.CopyFrom(mimeBytes),
            SaveInSentItems = saveInSent,
            ClientId = SubmissionKey(mimeBytes, clientId),
        };

        SubmitResponse response;
        try
        {
            response = await mail.SubmitAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "Mail submission failed for {UserId}", userId);
            return EasStatus.MailSubmissionFailed;
        }

        if (response.Status != SubmitStatus.Ok)
            logger.LogWarning("Submission for {UserId} was rejected with {Status}", userId, response.Status);

        return ToEasStatus(response.Status);
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

    // WP7 does not always send ClientId, so fall back to hashing the message: a resend of the same
    // bytes has to reuse the key or the store cannot tell it apart from a second message
    private static string SubmissionKey(byte[] mimeBytes, string? clientId) =>
        string.IsNullOrEmpty(clientId)
            ? "mime:" + Convert.ToHexString(SHA256.HashData(mimeBytes))
            : "client:" + clientId;

    private static int ToEasStatus(SubmitStatus status) => status switch
    {
        SubmitStatus.Ok => EasStatus.Success,
        SubmitStatus.NoRecipients => EasStatus.MessageHasNoRecipient,
        SubmitStatus.UnresolvedRecipients => EasStatus.MessageRecipientUnresolved,
        SubmitStatus.SenderNotAllowed => EasStatus.AccessDenied,
        SubmitStatus.TooLarge => EasStatus.AttachmentIsTooLarge,
        _ => EasStatus.MailSubmissionFailed,
    };

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
