using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Services;

// binds a contact to the live user who owns that email address, so the people hub can show their
// picture, presence and activity. Only writes the annotation, never the contact itself.
public class ContactLinkResolver(
    MailboxDbContext db,
    User.UserClient users,
    IConfiguration configuration,
    ILogger<ContactLinkResolver> logger)
{
    private const int BatchSize = 200;

    private readonly record struct LinkTarget(string UserId, string Cid, string Email, string? PictureEtag);

    // returns whether any address on this contact belongs to a live user at all, which is the cheap
    // gate for whether the reciprocal pass could possibly have work to do
    public async Task<bool> ResolveForContactAsync(DbContactItem contact, CancellationToken ct = default)
    {
        if (ContactAddresses.IsMeContact(contact))
            return false;

        var addresses = ContactAddresses.Of(contact).ToList();
        if (addresses.Count == 0)
        {
            await ApplyLink(contact, null, ct);
            return false;
        }

        LookupUsersByEmailResponse reply;
        try
        {
            reply = await users.LookupUsersByEmailAsync(
                new LookupUsersByEmailRequest { Emails = { addresses } }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "directory lookup failed for contact {Contact}, leaving its link alone", contact.ServerId);
            return false;
        }

        var matched = false;
        foreach (var candidate in reply.Users)
        {
            if (candidate.UserId == contact.UserId)
                continue;

            matched = true;

            if (!await IsDiscoverableAsync(contact.UserId, candidate.UserId, candidate.Visibility, ct))
                continue;

            await ApplyLink(contact, new LinkTarget(
                candidate.UserId,
                candidate.Cid,
                candidate.EmailAddress,
                candidate.HasPictureEtag ? candidate.PictureEtag : null), ct);

            return true;
        }

        await ApplyLink(contact, null, ct);
        return matched;
    }

    public async Task ReconcileForOwnerAsync(AccountProfileChangedEvent evt, CancellationToken ct = default)
    {
        if (ParseCid(evt.Cid) is not { } cid)
            return;

        var address = ContactAddresses.Normalize(evt.Email);
        var visibility = ParseVisibility(evt.Visibility);
        var target = new LinkTarget(evt.UserId, evt.Cid, evt.Email, evt.PictureEtag);

        var linked = await db.ContactAnnotations
            .Where(a => a.Cid == cid && !a.LinkIsManual)
            .Select(a => a.ContactItemId)
            .ToListAsync(ct);

        var carrying = address is null
            ? []
            : await db.ContactEmails
                .Where(e => e.NormalizedAddress == address && e.UserId != evt.UserId && e.ContactItem.DeletedAt == null)
                .Select(e => e.ContactItemId)
                .Distinct()
                .ToListAsync(ct);

        var ids = linked.Union(carrying).ToList();
        if (ids.Count == 0)
            return;

        var touched = 0;
        foreach (var batch in ids.Chunk(BatchSize))
        {
            var contacts = await db.Items.OfType<DbContactItem>()
                .Include(c => c.Annotation)
                .Where(c => batch.Contains(c.Id) && c.DeletedAt == null)
                .ToListAsync(ct);

            var changed = false;
            foreach (var contact in contacts)
            {
                var eligible = address is not null
                    && contact.UserId != evt.UserId
                    && !ContactAddresses.IsMeContact(contact)
                    && ContactAddresses.Of(contact).Contains(address, StringComparer.Ordinal)
                    && await IsDiscoverableAsync(contact.UserId, evt.UserId, visibility, ct);

                if (await ApplyLink(contact, eligible ? target : null, ct))
                {
                    changed = true;
                    touched++;
                }
            }

            if (changed)
                await db.SaveChangesAsync(ct);
        }

        if (touched > 0)
            logger.LogInformation("reconciled {Count} contact links for {User}", touched, evt.UserId);
    }

    // one user's address book changing only moves links where that user is the target, and only
    // when they are Mutual, since that is the only setting whose predicate reads their book
    public async Task ReconcileAddressBookOwnerAsync(string userId, CancellationToken ct = default)
    {
        if (await TryGetProfileAsync(userId, ct) is not { } evt)
            return;

        if (ParseVisibility(evt.Visibility) != ProfileVisibility.Mutual)
            return;

        await ReconcileForOwnerAsync(evt, ct);
    }

    public async Task ReconcileFromIdentityAsync(string userId, CancellationToken ct = default)
    {
        if (await TryGetProfileAsync(userId, ct) is { } evt)
            await ReconcileForOwnerAsync(evt, ct);
    }

    private async Task<AccountProfileChangedEvent?> TryGetProfileAsync(string userId, CancellationToken ct)
    {
        try
        {
            var profile = await users.GetUserProfileAsync(
                new GetUserProfileRequest { UserId = userId }, cancellationToken: ct);

            return new AccountProfileChangedEvent(
                profile.UserId,
                profile.Cid,
                profile.HasFirstName ? profile.FirstName : null,
                profile.HasLastName ? profile.LastName : null,
                profile.Username,
                profile.EmailAddress,
                profile.HasPictureEtag ? profile.PictureEtag : null,
                (profile.HasVisibility ? profile.Visibility : ProfileVisibility.Private).ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the profile for {User}, skipping its link pass", userId);
            return null;
        }
    }

    // the annotation only says the viewer has a contact carrying this cid. permission is worked out
    // again here every time, so a target going private takes effect without waiting for a reconcile
    public async Task<IReadOnlyDictionary<long, string>> ResolveDiscoverableSubjectsAsync(
        string viewerUserId, IReadOnlyCollection<long> cids, CancellationToken ct = default)
    {
        var resolved = new Dictionary<long, string>();
        if (cids.Count == 0)
            return resolved;

        // nullable so an annotation with no cid falls out of the IN without a separate guard
        var wanted = cids.Select(c => (long?)c).ToList();

        var candidates = await db.ContactAnnotations.AsNoTracking()
            .Where(a => wanted.Contains(a.Cid)
                && a.WLId != null
                && a.ContactItem.UserId == viewerUserId
                && a.ContactItem.DeletedAt == null)
            .Select(a => new { Cid = a.Cid!.Value, Address = a.WLId! })
            .Distinct()
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return resolved;

        LookupUsersByEmailResponse reply;
        try
        {
            reply = await users.LookupUsersByEmailAsync(
                new LookupUsersByEmailRequest { Emails = { candidates.Select(c => c.Address).Distinct() } },
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "directory lookup failed resolving feed subjects for {User}", viewerUserId);
            return resolved;
        }

        var byAddress = reply.Users
            .GroupBy(u => u.QueriedEmail, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (!byAddress.TryGetValue(candidate.Address, out var target))
                continue;

            if (target.UserId == viewerUserId)
                continue;

            // a stale WLId must not hand over a different account than the cid names
            if (ParseCid(target.Cid) != candidate.Cid)
                continue;

            if (!await IsDiscoverableAsync(viewerUserId, target.UserId, target.Visibility, ct))
                continue;

            resolved[candidate.Cid] = target.UserId;
        }

        return resolved;
    }

    public async Task<bool> IsDiscoverableAsync(string viewerUserId, string targetUserId, ProfileVisibility visibility, CancellationToken ct)
    {
        if (visibility == ProfileVisibility.Public)
            return true;

        if (visibility != ProfileVisibility.Mutual)
            return false;

        if (await AccountAddressAsync(viewerUserId, ct) is not { } viewerAddress)
            return false;

        return await db.ContactEmails.AnyAsync(
            e => e.UserId == targetUserId
                 && e.NormalizedAddress == viewerAddress
                 && e.ContactItem.DeletedAt == null, ct);
    }

    private async Task<string?> AccountAddressAsync(string userId, CancellationToken ct)
    {
        var address = await db.Items.OfType<DbContactItem>()
            .Where(c => c.UserId == userId && c.DeletedAt == null && c.Annotation!.ContactType == "Me")
            .Select(c => c.Email1Address)
            .FirstOrDefaultAsync(ct);

        return ContactAddresses.Normalize(address);
    }

    private async Task<bool> ApplyLink(DbContactItem contact, LinkTarget? target, CancellationToken ct)
    {
        var annotation = contact.Annotation;
        if (annotation is { LinkIsManual: true })
            return false;

        if (target is not { } link)
        {
            if (annotation is null || annotation.Cid is null)
                return false;

            // a cid that belongs to the social identity binding is not ours to take away
            if (await db.ContactIdentities.AnyAsync(i => i.ContactItemId == contact.Id && i.ContactCid == annotation.Cid, ct))
                return false;

            annotation.Cid = null;
            annotation.WLId = null;
            annotation.ImMri = null;
            annotation.UserTileUrl = null;
            annotation.UserTileHash = null;
            return true;
        }

        if (ParseCid(link.Cid) is not { } cid)
            return false;

        if (annotation is null)
        {
            annotation = new DbContactAnnotation { ContactItemId = contact.Id, ContactItem = contact };
            db.ContactAnnotations.Add(annotation);
            contact.Annotation = annotation;
        }

        var tileUrl = MeContactMirrorService.TileUrl(configuration, link.Cid, link.PictureEtag);
        var changed = false;

        void Set<T>(T current, T next, Action<T> assign)
        {
            if (EqualityComparer<T>.Default.Equals(current, next))
                return;

            assign(next);
            changed = true;
        }

        Set(annotation.Cid, cid, v => annotation.Cid = v);
        Set(annotation.WLId, link.Email, v => annotation.WLId = v);
        Set(annotation.ImMri, "1:" + link.Email, v => annotation.ImMri = v);
        Set(annotation.UserTileUrl, tileUrl, v => annotation.UserTileUrl = v);
        Set(annotation.UserTileHash, link.PictureEtag, v => annotation.UserTileHash = v);

        return changed;
    }

    private static long? ParseCid(string? cid) =>
        long.TryParse(cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static ProfileVisibility ParseVisibility(string? value) =>
        Enum.TryParse<ProfileVisibility>(value, ignoreCase: true, out var visibility)
            ? visibility
            : ProfileVisibility.Private;
}
