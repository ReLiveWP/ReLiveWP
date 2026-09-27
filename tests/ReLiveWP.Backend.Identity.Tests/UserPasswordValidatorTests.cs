using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity.Tests;

public class UserPasswordValidatorTests
{
    private static async Task<string[]> ValidatePasswordAsync(string password, LiveUserType type = LiveUserType.User)
    {
        var user = new LiveUser() { Type = type };
        var result = await new UserPasswordValidator().ValidateAsync(null!, user, password);
        return [.. result.Errors.Select(e => e.Code)];
    }

    [Theory]
    [InlineData("hunter22")]
    [InlineData("correct horse battery staple")]
    public async Task Typeable_passwords_of_eight_or_more_are_fine(string password)
    {
        var errors = await ValidatePasswordAsync(password);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0x00E4)]
    [InlineData(0x00F6)]
    [InlineData(0x00FF)]
    public async Task Latin1_letters_are_fine(int codePoint)
    {
        var errors = await ValidatePasswordAsync("hunter22" + (char)codePoint);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Seven_characters_is_too_short()
    {
        var errors = await ValidatePasswordAsync("hunter2");

        Assert.Equal([UserPasswordValidator.TooShortCode], errors);
    }

    [Fact]
    public async Task One_hundred_and_twenty_seven_characters_is_the_limit()
    {
        var atLimit = await ValidatePasswordAsync(new string('a', 127));
        var overLimit = await ValidatePasswordAsync(new string('a', 128));

        Assert.Empty(atLimit);
        Assert.Equal([UserPasswordValidator.TooLongCode], overLimit);
    }

    [Theory]
    [InlineData(0x0100)]
    [InlineData(0x2603)]
    [InlineData(0x0009)]
    [InlineData(0x0085)]
    public async Task Characters_the_phone_cannot_send_are_rejected(int codePoint)
    {
        var errors = await ValidatePasswordAsync("hunter22" + (char)codePoint);

        Assert.Equal([UserPasswordValidator.InvalidCharactersCode], errors);
    }

    [Fact]
    public async Task Device_accounts_are_left_alone()
    {
        var errors = await ValidatePasswordAsync("x", LiveUserType.Device);

        Assert.Empty(errors);
    }
}
