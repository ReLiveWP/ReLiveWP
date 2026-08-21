using ReLiveWP.Mail;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mail.Services;

public class LocalDeliveryAgent(
    MailboxStore.MailboxStoreClient mailbox,
    MimeIngest ingest,
    ILogger<LocalDeliveryAgent> logger) : IMailDeliveryAgent
{
    public MailRoute Route => MailRoute.Local;

    public async Task DeliverAsync(MailEnvelope envelope, byte[] message, CancellationToken ct)
    {
        foreach (var recipient in envelope.Recipients)
        {
            if (recipient.UserId is null)
                continue;

            var item = ingest.ToEmailItem(message, envelope.MailFrom);

            // ClientId is unique per (user, folder), so one submission id covers every recipient
            // and a redelivered queue entry lands exactly once in each mailbox
            await mailbox.DeliverEmailAsync(
                new DeliverEmailRequest
                {
                    UserId = recipient.UserId,
                    Email = item,
                    ClientId = $"submission:{envelope.SubmissionId}",
                },
                cancellationToken: ct);

            logger.LogInformation(
                "Delivered submission {SubmissionId} to {Address}", envelope.SubmissionId, recipient.Address);
        }
    }
}
