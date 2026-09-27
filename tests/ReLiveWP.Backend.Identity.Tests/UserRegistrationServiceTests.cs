using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity.Tests;

public class UserRegistrationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LiveDbContext _db;

    public UserRegistrationServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _db = new LiveDbContext(
            new DbContextOptionsBuilder<LiveDbContext>().UseSqlite(_connection).Options);

        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private UserManager<LiveUser> CreateUserManager()
    {
        var options = new IdentityOptions();
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 1;

        return new UserManager<LiveUser>(
            new UserStore<LiveUser, LiveRole, LiveDbContext, Guid>(_db),
            Options.Create(options),
            new PasswordHasher<LiveUser>(),
            [new UserValidator<LiveUser>()],
            [new PasswordValidator<LiveUser>(), new UserPasswordValidator()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<LiveUser>>.Instance);
    }

    private UserRegistrationService CreateService(IInviteCodeValidator? inviteCodeValidator = null)
        => new(CreateUserManager(), _db, inviteCodeValidator ?? new PermissiveInviteCodeValidator());

    private UserRegistrationService CreateInviteOnlyService()
        => CreateService(new FakeInviteCodeValidator());

    private static UserRegistration Signup(string name, string? activationCodeHash = null, string? inviteCode = null)
        => new(name, name, "hunter22", "backup@example.com", activationCodeHash, inviteCode);

    private sealed class FakeInviteCodeValidator : IInviteCodeValidator
    {
        public InviteCodeCheck CheckInviteCode(string inviteCode) => inviteCode switch
        {
            "INVITE-7" => new(true, 7),
            "INVITE-8" => new(true, 8),
            _ => new(false, null),
        };
    }

    [Fact]
    public async Task Unused_name_is_available()
    {
        var code = await CreateService().CheckUserNameAvailabilityAsync("fresh@relivewp.net");

        Assert.Equal(UserRegistrationService.S_OK, code);
    }

    [Fact]
    public async Task Name_taken_as_a_username_is_unavailable()
    {
        var service = CreateService();
        await service.RegisterUserAsync(Signup("taken@relivewp.net"));

        var code = await service.CheckUserNameAvailabilityAsync("taken@relivewp.net");

        Assert.Equal(UserRegistrationService.MEMBER_EXISTS, code);
    }

    [Fact]
    public async Task Name_taken_as_someone_elses_email_is_unavailable()
    {
        var service = CreateService();
        await service.RegisterUserAsync(new UserRegistration("someone", "someone@relivewp.net", "hunter22"));

        var code = await service.CheckUserNameAvailabilityAsync("someone@relivewp.net");

        Assert.Equal(UserRegistrationService.MEMBER_EXISTS, code);
    }

    [Theory]
    [InlineData("has space@relivewp.net")]
    [InlineData("amp&ersand@relivewp.net")]
    [InlineData("no-at-sign")]
    public async Task Malformed_name_is_invalid(string name)
    {
        var code = await CreateService().CheckUserNameAvailabilityAsync(name);

        Assert.Equal(UserRegistrationService.MEMBER_INVALID, code);
    }

    [Fact]
    public async Task Register_stores_the_signup_fields()
    {
        var result = await CreateService().RegisterUserAsync(Signup("new@relivewp.net", "hash-a"));

        Assert.Equal(UserRegistrationService.S_OK, result.Code);
        var stored = await _db.Users.SingleAsync(u => u.UserName == "new@relivewp.net");
        Assert.Equal("new@relivewp.net", stored.Email);
        Assert.Equal("backup@example.com", stored.AlternateEmail);
        Assert.Equal("hash-a", stored.SignupActivationCodeHash);
        Assert.Equal(LiveUserType.User, stored.Type);
    }

    [Fact]
    public async Task Register_rejects_a_taken_name()
    {
        var service = CreateService();
        await service.RegisterUserAsync(Signup("dupe@relivewp.net"));

        var result = await service.RegisterUserAsync(Signup("dupe@relivewp.net"));

        Assert.Equal(UserRegistrationService.MEMBER_EXISTS, result.Code);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Register_rejects_a_second_account_for_the_same_activation_code()
    {
        var service = CreateService();
        await service.RegisterUserAsync(Signup("first@relivewp.net", "hash-b"));

        var result = await service.RegisterUserAsync(Signup("second@relivewp.net", "hash-b"));

        Assert.Equal(UserRegistrationService.ACCOUNTDENIED, result.Code);
        Assert.False(await _db.Users.AnyAsync(u => u.UserName == "second@relivewp.net"));
    }

    [Fact]
    public async Task Accounts_without_an_activation_code_do_not_collide()
    {
        var service = CreateService();
        await service.RegisterUserAsync(Signup("one@relivewp.net"));

        var result = await service.RegisterUserAsync(Signup("two@relivewp.net"));

        Assert.Equal(UserRegistrationService.S_OK, result.Code);
    }

    [Fact]
    public async Task Register_rejects_a_malformed_name()
    {
        var result = await CreateService().RegisterUserAsync(Signup("bad name@relivewp.net"));

        Assert.Equal(UserRegistrationService.MEMBER_INVALID, result.Code);
    }

    [Fact]
    public async Task Database_refuses_two_users_with_the_same_activation_code()
    {
        _db.Users.Add(RawUser("race-a@relivewp.net", "hash-c"));
        _db.Users.Add(RawUser("race-b@relivewp.net", "hash-c"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOT-AN-INVITE")]
    public async Task Register_rejects_a_bad_invite(string? inviteCode)
    {
        var result = await CreateInviteOnlyService().RegisterUserAsync(Signup("uninvited@relivewp.net", inviteCode: inviteCode));

        Assert.Equal(UserRegistrationService.GWP_E_ACTIVATION_CODE_INVALID, result.Code);
        Assert.False(await _db.Users.AnyAsync(u => u.UserName == "uninvited@relivewp.net"));
    }

    [Fact]
    public async Task Register_stores_the_invite_serial()
    {
        var result = await CreateInviteOnlyService().RegisterUserAsync(Signup("invited@relivewp.net", inviteCode: "INVITE-7"));

        Assert.Equal(UserRegistrationService.S_OK, result.Code);
        var stored = await _db.Users.SingleAsync(u => u.UserName == "invited@relivewp.net");
        Assert.Equal(7, stored.InviteSerial);
    }

    [Fact]
    public async Task Register_rejects_a_second_account_for_the_same_invite()
    {
        var service = CreateInviteOnlyService();
        await service.RegisterUserAsync(Signup("first-invited@relivewp.net", inviteCode: "INVITE-8"));

        var result = await service.RegisterUserAsync(Signup("second-invited@relivewp.net", inviteCode: "INVITE-8"));

        Assert.Equal(UserRegistrationService.GWP_E_ACTIVATION_CODE_IN_USE, result.Code);
        Assert.False(await _db.Users.AnyAsync(u => u.UserName == "second-invited@relivewp.net"));
    }

    [Fact]
    public async Task Different_invites_do_not_collide()
    {
        var service = CreateInviteOnlyService();
        await service.RegisterUserAsync(Signup("seven@relivewp.net", inviteCode: "INVITE-7"));

        var result = await service.RegisterUserAsync(Signup("eight@relivewp.net", inviteCode: "INVITE-8"));

        Assert.Equal(UserRegistrationService.S_OK, result.Code);
    }

    [Fact]
    public async Task Register_reports_a_short_password_as_a_live_code()
    {
        var registration = new UserRegistration("short@relivewp.net", "short@relivewp.net", "short");

        var result = await CreateService().RegisterUserAsync(registration);

        Assert.Equal(UserRegistrationService.PASSWORD_TOOSHORT, result.Code);
    }

    [Fact]
    public async Task Register_reports_an_untypeable_password_as_a_live_code()
    {
        var snowman = (char)0x2603;
        var registration = new UserRegistration("snow@relivewp.net", "snow@relivewp.net", "hunter22" + snowman);

        var result = await CreateService().RegisterUserAsync(registration);

        Assert.Equal(UserRegistrationService.PASSWORD_INVALIDCHARS, result.Code);
    }

    [Fact]
    public async Task Database_refuses_two_users_with_the_same_invite_serial()
    {
        var first = RawUser("serial-a@relivewp.net");
        var second = RawUser("serial-b@relivewp.net");
        first.InviteSerial = 42;
        second.InviteSerial = 42;
        _db.Users.Add(first);
        _db.Users.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    private static LiveUser RawUser(string name, string? activationCodeHash = null)
    {
        var (userId, cid, puid) = UserUtils.GenerateUserIds(LiveUserType.User);
        return new LiveUser()
        {
            Id = userId,
            Cid = cid,
            Puid = puid,
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            Email = name,
            SignupActivationCodeHash = activationCodeHash,
        };
    }
}
