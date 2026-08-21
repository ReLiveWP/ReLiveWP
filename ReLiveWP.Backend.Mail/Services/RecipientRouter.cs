using Microsoft.Extensions.Options;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mail.Services;

public class RecipientRouter(User.UserClient users, IOptions<MailOptions> options) : IRecipientRouter
{
    private readonly HashSet<string> localDomains =
        new(options.Value.LocalDomains, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ResolvedRecipient>> ResolveAsync(
        IReadOnlyList<string> addresses, CancellationToken ct)
    {
        // escape hatch, not all relive users have @relivewp.net addresses, so every recipient has
        // to be offered to the directory rather than just the ones on a domain we own
        // BUGBUG: get rid of this, enforce username@relivewp.net
        var candidates = options.Value.VerifyLocalDomains
            ? addresses.Where(IsLocalDomain).ToArray()
            : addresses.ToArray();

        var mailboxes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (candidates.Length > 0)
        {
            // a private profile still has a mailbox, so private users have to resolve here
            var request = new LookupUsersByEmailRequest { IncludePrivate = true };
            request.Emails.AddRange(candidates);

            var response = await users.LookupUsersByEmailAsync(request, cancellationToken: ct);
            foreach (var user in response.Users)
                mailboxes[user.QueriedEmail] = user.UserId;
        }

        return [.. addresses.Select(address => Resolve(address, mailboxes))];
    }

    private ResolvedRecipient Resolve(string address, IReadOnlyDictionary<string, string> mailboxes)
    {
        if (mailboxes.TryGetValue(address, out var userId))
            return new ResolvedRecipient(address, MailRoute.Local, userId);

        // no mailbox on a domain we own means nobody to deliver to; anywhere else is somebody
        // else's problem to route
        return IsLocalDomain(address)
            ? new ResolvedRecipient(address, MailRoute.Unroutable, null)
            : new ResolvedRecipient(address, MailRoute.External, null);
    }

    private bool IsLocalDomain(string address)
    {
        var at = address.LastIndexOf('@');
        return at >= 0 && localDomains.Contains(address[(at + 1)..]);
    }
}
