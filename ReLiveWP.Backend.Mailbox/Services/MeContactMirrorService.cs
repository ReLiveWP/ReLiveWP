using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Services;

// pushes the account profile onto the EAS Me contact. Writes through the DbContext rather than the
// MailboxStore RPC so ApplyToEntity's whole-record overwrite is not in the way.
public class MeContactMirrorService(
    MailboxDbContext db,
    User.UserClient users,
    IConfiguration configuration,
    ILogger<MeContactMirrorService> logger)
{
    // for the provisioning path, which knows a user id and nothing else
    public async Task MirrorFromIdentityAsync(string userId, CancellationToken ct = default)
    {
        UserProfile profile;
        try
        {
            profile = await users.GetUserProfileAsync(new GetUserProfileRequest { UserId = userId }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the profile for {User}, me contact keeps its seeded values", userId);
            return;
        }

        await MirrorAsync(new AccountProfileChangedEvent(
            profile.UserId,
            profile.Cid,
            profile.HasFirstName ? profile.FirstName : null,
            profile.HasLastName ? profile.LastName : null,
            profile.Username,
            profile.EmailAddress,
            profile.HasPictureEtag ? profile.PictureEtag : null), ct);
    }

    public async Task MirrorAsync(AccountProfileChangedEvent evt, CancellationToken ct = default)
    {
        var contact = await db.Items.OfType<DbContactItem>()
            .Include(c => c.Annotation)
            .FirstOrDefaultAsync(c => c.UserId == evt.UserId
                                      && c.DeletedAt == null
                                      && c.Annotation!.ContactType == "Me", ct);

        if (contact is null)
        {
            logger.LogInformation("no me contact for {User}, nothing to mirror", evt.UserId);
            return;
        }

        var fields = MeContactFields.From(evt.FirstName, evt.LastName, evt.Username, evt.Email);
        var tileUrl = TileUrl(configuration, evt.Cid, evt.PictureEtag);

        var changed = Apply(contact, fields);

        var annotation = contact.Annotation!;
        if (annotation.UserTileUrl != tileUrl || annotation.UserTileHash != evt.PictureEtag)
        {
            annotation.UserTileUrl = tileUrl;
            annotation.UserTileHash = evt.PictureEtag;
            changed = true;
        }

        if (evt.PictureEtag is null)
        {
            if (contact.Picture is not null)
            {
                contact.Picture = null;
                changed = true;
            }
        }
        else if (await FetchThumbnailAsync(evt.UserId, ct) is { } picture && !PictureEquals(contact.Picture, picture))
        {
            contact.Picture = picture;
            changed = true;
        }

        // a no-op save would still emit an item event and wake the device for nothing, and it is
        // what stops the mirror and the device writeback bouncing off each other
        if (!changed)
            return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("mirrored profile onto me contact {Contact} for {User}", contact.ServerId, evt.UserId);
    }

    private static bool Apply(DbContactItem contact, MeContactFields fields)
    {
        var changed = false;

        void Set(string? current, string? next, Action<string?> assign)
        {
            if (current == next)
                return;

            assign(next);
            changed = true;
        }

        Set(contact.FirstName, fields.FirstName, v => contact.FirstName = v);
        Set(contact.LastName, fields.LastName, v => contact.LastName = v);
        Set(contact.FileAs, fields.FileAs, v => contact.FileAs = v);
        Set(contact.Email1Address, fields.Email, v => contact.Email1Address = v);
        Set(contact.IMAddress, fields.Email, v => contact.IMAddress = v);
        Set(contact.IMAddress2, fields.Mri, v => contact.IMAddress2 = v);

        return changed;
    }

    private async Task<byte[]?> FetchThumbnailAsync(string userId, CancellationToken ct)
    {
        try
        {
            var reply = await users.GetUserPictureAsync(
                new GetUserPictureRequest { UserId = userId, Thumbnail = true }, cancellationToken: ct);
            return reply.Data.ToByteArray();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not fetch the avatar thumbnail for {User}", userId);
            return null;
        }
    }

    private static bool PictureEquals(byte[]? a, byte[]? b) =>
        a is not null && b is not null && a.AsSpan().SequenceEqual(b);

    public static string? TileUrl(IConfiguration configuration, string cid, string? etag)
    {
        if (etag is null)
            return null;

        var root = configuration["PublicUrls:Avatars"];
        return string.IsNullOrWhiteSpace(root) ? null : $"{root.TrimEnd('/')}/{cid}/picture";
    }
}

public readonly record struct MeContactFields(string? FirstName, string? LastName, string FileAs, string Email, string Mri)
{
    public static MeContactFields From(string? firstName, string? lastName, string username, string email)
    {
        var first = Blank(firstName);
        var last = Blank(lastName);
        var fileAs = string.Join(' ', new[] { first, last }.Where(s => s is not null));

        // no name on the profile yet, so fall back to the username the mailbox was seeded with
        if (fileAs.Length == 0)
        {
            first = username;
            fileAs = username;
        }

        return new MeContactFields(first, last, fileAs, email, "1:" + email);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
