using Grpc.Core;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Mail.Services;
using ReLiveWP.Mail;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mail;
using MailBase = ReLiveWP.Services.Grpc.Mail.Mail.MailBase;

namespace ReLiveWP.Backend.Mail.Grpc;

public class MailSubmissionService(
    IRecipientRouter router,
    IMailQueue queue,
    ISentItemsWriter sentItems,
    IEnumerable<IMailDeliveryAgent> agents,
    User.UserClient users,
    IOptions<MailOptions> options,
    ILogger<MailSubmissionService> logger) : MailBase
{
    private readonly HashSet<MailRoute> deliverable = [.. agents.Select(a => a.Route)];

    public override async Task<SubmitResponse> Submit(SubmitRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;

        if (string.IsNullOrEmpty(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));

        if (request.Mime.Length > options.Value.MaxMessageBytes)
            return new SubmitResponse { Status = SubmitStatus.TooLarge };

        if (!MimeSubmission.TryPrepare(request.Mime.ToByteArray(), options.Value.MessageIdDomain, out var prepared))
            return new SubmitResponse { Status = SubmitStatus.Malformed };

        if (prepared.Recipients.Count == 0)
            return new SubmitResponse { Status = SubmitStatus.NoRecipients };

        var sender = await SenderAddressAsync(request.UserId, ct);
        if (!OwnsFromAddress(sender, prepared.From))
        {
            logger.LogWarning(
                "Rejected submission from {UserId}: From was {From}, expected {Sender}",
                request.UserId, prepared.From, sender);
            return new SubmitResponse { Status = SubmitStatus.SenderNotAllowed };
        }

        var recipients = await router.ResolveAsync(prepared.Recipients, ct);
        var response = new SubmitResponse();
        response.Recipients.AddRange(recipients.Select(ToResult));

        if (recipients.Any(r => !deliverable.Contains(r.Route)))
        {
            logger.LogWarning(
                "Rejected submission from {UserId}: Not all recipients could be resolved.",
                request.UserId);
            response.Status = SubmitStatus.UnresolvedRecipients;
            return response;
        }

        var submissionId = request.HasClientId && !string.IsNullOrEmpty(request.ClientId)
            ? request.ClientId
            : Guid.NewGuid().ToString("N");

        var envelope = new MailEnvelope(submissionId, request.UserId, sender, recipients);

        // queue first: if the sender copy fails the message still gets delivered, which is the
        // better half to lose
        await queue.EnqueueAsync(envelope, prepared.DeliveryBytes, ct);

        if (request.SaveInSentItems)
        {
            try
            {
                await sentItems.WriteAsync(request.UserId, submissionId, sender, prepared.SentItemsBytes, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sender copy failed for submission {SubmissionId}", submissionId);
            }
        }

        response.Status = SubmitStatus.Ok;
        response.SubmissionId = submissionId;
        return response;
    }

    private async Task<string> SenderAddressAsync(string userId, CancellationToken ct)
    {
        var info = await users.GetUserInfoAsync(new GetUserInfoRequest { UserId = userId }, cancellationToken: ct);
        return info.EmailAddress;
    }

    // an absent From is fine, the sender's address is stamped on the stored item either way;
    // a present one has to be theirs or this is an impersonation attempt
    private static bool OwnsFromAddress(string sender, string? from) =>
        string.IsNullOrEmpty(from) || string.Equals(from, sender, StringComparison.OrdinalIgnoreCase);

    private static RecipientResult ToResult(ResolvedRecipient recipient) => new()
    {
        Address = recipient.Address,
        Route = recipient.Route switch
        {
            MailRoute.Local => RecipientRoute.Local,
            MailRoute.External => RecipientRoute.External,
            _ => RecipientRoute.Unroutable,
        },
    };
}
