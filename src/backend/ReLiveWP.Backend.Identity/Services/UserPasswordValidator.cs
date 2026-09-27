using Microsoft.AspNetCore.Identity;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public class UserPasswordValidator : IPasswordValidator<LiveUser>
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 127;

    public const string TooShortCode = nameof(IdentityErrorDescriber.PasswordTooShort);
    public const string TooLongCode = "PasswordTooLong";
    public const string InvalidCharactersCode = "PasswordInvalidCharacters";

    public Task<IdentityResult> ValidateAsync(UserManager<LiveUser> manager, LiveUser user, string? password)
    {
        if (user.Type != LiveUserType.User || password == null)
            return Task.FromResult(IdentityResult.Success);

        var errors = new List<IdentityError>();

        if (password.Length < MinimumLength)
            errors.Add(new IdentityError() { Code = TooShortCode, Description = $"Passwords must be at least {MinimumLength} characters." });

        if (password.Length > MaximumLength)
            errors.Add(new IdentityError() { Code = TooLongCode, Description = $"Passwords must be at most {MaximumLength} characters." });

        if (!password.All(IsDeviceTypeableCharacter))
            errors.Add(new IdentityError() { Code = InvalidCharactersCode, Description = "Passwords may only contain printable Latin-1 characters." });

        var result = errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]);
        return Task.FromResult(result);
    }

    private static bool IsDeviceTypeableCharacter(char character)
        => character < 0x100 && !char.IsControl(character);
}
