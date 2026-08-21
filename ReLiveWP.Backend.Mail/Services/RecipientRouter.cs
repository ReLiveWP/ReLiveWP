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
        var local = addresses.Where(IsLocalDomain).ToArray();
        var mailboxes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (local.Length > 0)
        {
            // a private profile still has a mailbox, so private users have to resolve here
            var request = new LookupUsersByEmailRequest { IncludePrivate = true };
            request.Emails.AddRange(local);

            var response = await users.LookupUsersByEmailAsync(request, cancellationToken: ct);
            foreach (var user in response.Users)
                mailboxes[user.QueriedEmail] = user.UserId;
        }

        return [.. addresses.Select(address => Resolve(address, mailboxes))];
    }

    private ResolvedRecipient Resolve(string address, IReadOnlyDictionary<string, string> mailboxes)
    {
        if (!IsLocalDomain(address))
            return new ResolvedRecipient(address, MailRoute.External, null);

        return mailboxes.TryGetValue(address, out var userId)
            ? new ResolvedRecipient(address, MailRoute.Local, userId)
            : new ResolvedRecipient(address, MailRoute.Unroutable, null);
    }

    private bool IsLocalDomain(string address)
    {
        var at = address.LastIndexOf('@');
        return at >= 0 && localDomains.Contains(address[(at + 1)..]);
    }
}
