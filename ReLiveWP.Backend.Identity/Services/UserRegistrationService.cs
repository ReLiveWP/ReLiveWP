using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public record UserRegistration(
    string UserName,
    string EmailAddress,
    string Password,
    string? AlternateEmail = null,
    string? ActivationCodeHash = null);

public record UserRegistrationResult(uint Code, LiveUser? User);

public class UserRegistrationService(UserManager<LiveUser> userManager, LiveDbContext dbContext)
{
    public const uint S_OK = 0x0;
    public const uint E_FAIL = 0x80004005;
    public const uint ERROR_ALREADY_EXISTS = 0x800700B7;
    public const uint ERROR_INVALID_PASSWORD = 0x80070056;
    public const uint ERROR_INVALID_ACCOUNT_NAME = 0x80070523;
    public const uint ERROR_QUOTA_EXCEEDED = 0x80070718;

    public async Task<uint> CheckUserNameAvailabilityAsync(string userName)
    {
        var candidate = new LiveUser() { UserName = userName, Email = userName };

        var errors = new List<IdentityError>();
        foreach (var validator in userManager.UserValidators)
        {
            var result = await validator.ValidateAsync(userManager, candidate);
            errors.AddRange(result.Errors);
        }

        return errors.Count == 0 ? S_OK : MapIdentityErrors(errors);
    }

    public async Task<UserRegistrationResult> RegisterUserAsync(UserRegistration registration)
    {
        if (await userManager.FindByNameAsync(registration.UserName) != null)
            return new(ERROR_ALREADY_EXISTS, null);

        if (registration.ActivationCodeHash is { } activationCodeHash
            && await IsActivationCodeHashUsedAsync(activationCodeHash))
            return new(ERROR_QUOTA_EXCEEDED, null);

        var (userId, cid, puid) = UserUtils.GenerateUserIds(LiveUserType.User);
        var user = new LiveUser()
        {
            Id = userId,
            Cid = cid,
            Puid = puid,
            UserName = registration.UserName,
            Email = registration.EmailAddress,
            AlternateEmail = registration.AlternateEmail,
            SignupActivationCodeHash = registration.ActivationCodeHash,
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, registration.Password);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(user).State = EntityState.Detached;

            var raceCode = await FindRaceLossCodeAsync(registration);
            if (raceCode == null)
                throw;

            return new(raceCode.Value, null);
        }

        if (!result.Succeeded)
            return new(MapIdentityErrors(result.Errors), null);

        return new(S_OK, user);
    }

    private async Task<uint?> FindRaceLossCodeAsync(UserRegistration registration)
    {
        if (registration.ActivationCodeHash is { } activationCodeHash
            && await IsActivationCodeHashUsedAsync(activationCodeHash))
            return ERROR_QUOTA_EXCEEDED;

        if (await userManager.FindByNameAsync(registration.UserName) != null)
            return ERROR_ALREADY_EXISTS;

        return null;
    }

    private Task<bool> IsActivationCodeHashUsedAsync(string activationCodeHash)
        => dbContext.Users.AnyAsync(u => u.SignupActivationCodeHash == activationCodeHash);

    private static uint MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(e => e.Code).ToHashSet();

        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName))
            || codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)))
            return ERROR_ALREADY_EXISTS;

        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidUserName))
            || codes.Contains(nameof(IdentityErrorDescriber.InvalidEmail)))
            return ERROR_INVALID_ACCOUNT_NAME;

        if (codes.Any(code => code.StartsWith("Password", StringComparison.Ordinal)))
            return ERROR_INVALID_PASSWORD;

        return E_FAIL;
    }
}
