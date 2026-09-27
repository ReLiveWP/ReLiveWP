using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public record UserRegistration(
    string UserName,
    string EmailAddress,
    string Password,
    string? AlternateEmail = null,
    string? ActivationCodeHash = null,
    string? InviteCode = null);

public record UserRegistrationResult(uint Code, LiveUser? User);

public class UserRegistrationService(
    UserManager<LiveUser> userManager,
    LiveDbContext dbContext,
    IInviteCodeValidator inviteCodeValidator)
{
    public const uint S_OK = 0x0;
    public const uint E_FAIL = 0x80004005;
    public const uint MEMBER_INVALID = 0x80041103;
    public const uint PASSWORD_TOOSHORT = 0x80041105;
    public const uint PASSWORD_TOOLONG = 0x80041106;
    public const uint PASSWORD_INVALIDCHARS = 0x80041108;
    public const uint MEMBER_EXISTS = 0x80041133;
    public const uint ACCOUNTDENIED = 0x800488B9;
    public const uint GWP_E_ACTIVATION_CODE_INVALID = 0x81120003;
    public const uint GWP_E_ACTIVATION_CODE_IN_USE = 0x81120005;

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
        var inviteCheck = inviteCodeValidator.CheckInviteCode(registration.InviteCode ?? "");
        if (!inviteCheck.IsValid)
            return new(GWP_E_ACTIVATION_CODE_INVALID, null);

        if (await userManager.FindByNameAsync(registration.UserName) != null)
            return new(MEMBER_EXISTS, null);

        if (registration.ActivationCodeHash is { } activationCodeHash
            && await IsActivationCodeHashUsedAsync(activationCodeHash))
            return new(ACCOUNTDENIED, null);

        if (inviteCheck.Serial is { } inviteSerial
            && await IsInviteSerialUsedAsync(inviteSerial))
            return new(GWP_E_ACTIVATION_CODE_IN_USE, null);

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
            InviteSerial = inviteCheck.Serial,
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, registration.Password);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(user).State = EntityState.Detached;

            var raceCode = await FindRaceLossCodeAsync(registration, inviteCheck.Serial);
            if (raceCode == null)
                throw;

            return new(raceCode.Value, null);
        }

        if (!result.Succeeded)
            return new(MapIdentityErrors(result.Errors), null);

        return new(S_OK, user);
    }

    private async Task<uint?> FindRaceLossCodeAsync(UserRegistration registration, int? inviteSerial)
    {
        if (registration.ActivationCodeHash is { } activationCodeHash
            && await IsActivationCodeHashUsedAsync(activationCodeHash))
            return ACCOUNTDENIED;

        if (inviteSerial is { } serial && await IsInviteSerialUsedAsync(serial))
            return GWP_E_ACTIVATION_CODE_IN_USE;

        if (await userManager.FindByNameAsync(registration.UserName) != null)
            return MEMBER_EXISTS;

        return null;
    }

    private Task<bool> IsActivationCodeHashUsedAsync(string activationCodeHash)
        => dbContext.Users.AnyAsync(u => u.SignupActivationCodeHash == activationCodeHash);

    private Task<bool> IsInviteSerialUsedAsync(int inviteSerial)
        => dbContext.Users.AnyAsync(u => u.InviteSerial == inviteSerial);

    private static uint MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(e => e.Code).ToHashSet();

        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName))
            || codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)))
            return MEMBER_EXISTS;

        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidUserName))
            || codes.Contains(nameof(IdentityErrorDescriber.InvalidEmail)))
            return MEMBER_INVALID;

        if (codes.Contains(UserPasswordValidator.InvalidCharactersCode))
            return PASSWORD_INVALIDCHARS;

        if (codes.Contains(UserPasswordValidator.TooLongCode))
            return PASSWORD_TOOLONG;

        if (codes.Any(code => code.StartsWith("Password", StringComparison.Ordinal)))
            return PASSWORD_TOOSHORT;

        return E_FAIL;
    }
}
