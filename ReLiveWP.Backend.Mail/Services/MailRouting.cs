namespace ReLiveWP.Backend.Mail.Services;

public enum MailRoute
{
    Local,
    External,
    Unroutable,
}

public sealed record ResolvedRecipient(string Address, MailRoute Route, string? UserId);

public sealed record MailEnvelope(
    string SubmissionId,
    string UserId,
    string MailFrom,
    IReadOnlyList<ResolvedRecipient> Recipients);

public interface IRecipientRouter
{
    Task<IReadOnlyList<ResolvedRecipient>> ResolveAsync(IReadOnlyList<string> addresses, CancellationToken ct);
}

// one implementation per route; the router picks which one a recipient belongs to. phase 3 adds
// an SMTP agent here and nothing upstream changes.
public interface IMailDeliveryAgent
{
    MailRoute Route { get; }

    Task DeliverAsync(MailEnvelope envelope, byte[] message, CancellationToken ct);
}
