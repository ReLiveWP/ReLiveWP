using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

// device edits to the me contact's name and picture go back to the account. Addresses do not:
// ItemSyncService.ApplyContactChange restores those before the write ever reaches the store.
public class MeProfileWriteback(User.UserClient users, ILogger<MeProfileWriteback> logger)
{
    public async Task WriteBackAsync(string userId, ContactItem before, ContactItem after, CancellationToken ct)
    {
        try
        {
            var firstChanged = Name(before.HasFirstName, before.FirstName) != Name(after.HasFirstName, after.FirstName);
            var lastChanged = Name(before.HasLastName, before.LastName) != Name(after.HasLastName, after.LastName);

            if (firstChanged || lastChanged)
            {
                var request = new UpdateUserProfileRequest { UserId = userId };
                if (after.HasFirstName) request.FirstName = after.FirstName;
                if (after.HasLastName) request.LastName = after.LastName;

                await users.UpdateUserProfileAsync(request, cancellationToken: ct);
                logger.LogInformation("me contact name written back to the account for {User}", userId);
            }

            if (!PictureChanged(before, after))
                return;

            if (!after.HasPicture || after.Picture.Length == 0)
            {
                await users.ClearUserPictureAsync(new ClearUserPictureRequest { UserId = userId }, cancellationToken: ct);
                logger.LogInformation("me contact picture cleared on the account for {User}", userId);
                return;
            }

            await users.SetUserPictureAsync(new SetUserPictureRequest
            {
                UserId = userId,
                Data = after.Picture,
            }, cancellationToken: ct);

            logger.LogInformation("me contact picture written back to the account for {User}", userId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // the device's change is already committed to the mailbox, so failing the Sync over this
            // would be worse than the profile lagging
            logger.LogWarning(ex, "could not write the me contact back to the account for {User}", userId);
        }
    }

    private static string? Name(bool has, string value) => has ? value : null;

    private static bool PictureChanged(ContactItem before, ContactItem after)
    {
        if (before.HasPicture != after.HasPicture)
            return true;

        return before.HasPicture && before.Picture != after.Picture;
    }
}
