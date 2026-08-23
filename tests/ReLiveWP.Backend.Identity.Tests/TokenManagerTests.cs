using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity.Tests;

public class TokenManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LiveDbContext _db;

    public TokenManagerTests()
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

    private TokenManager Manager()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"] = "a-test-signing-secret-that-is-long-enough-for-hmac-sha256",
            })
            .Build();

        return new TokenManager(configuration, NullLogger<TokenManager>.Instance, UserManager(), _db);
    }

    private UserManager<LiveUser> UserManager()
        => new(new UserStore<LiveUser, LiveRole, LiveDbContext, Guid>(_db),
            null!, new PasswordHasher<LiveUser>(), [], [],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            NullLogger<UserManager<LiveUser>>.Instance);

    private static LiveUser User() => new()
    {
        Id = Guid.NewGuid(),
        Cid = "0123456789abcdef",
        Puid = 0x99B2C15D02A28,
        UserName = "someone",
        Email = "someone@relivewp.net",
        Type = LiveUserType.User,
    };

    [Fact]
    public void Jwt_with_no_lifetime_still_lasts_thirty_days()
    {
        var issued = Manager().IssueJwtAsync(User(), "relivewp.net");

        var days = (issued.Expires - issued.Created).TotalDays;
        Assert.InRange(days, 29.9, 30.1);
    }

    [Fact]
    public void Jwt_honours_an_explicit_lifetime()
    {
        var issued = Manager().IssueJwtAsync(User(), "relivewp.net", TimeSpan.FromHours(1));

        var minutes = (issued.Expires - issued.Created).TotalMinutes;
        Assert.InRange(minutes, 59, 61);
    }

    [Fact]
    public void Jwt_carries_sid_only_when_a_session_is_supplied()
    {
        var manager = Manager();
        var sessionId = Guid.NewGuid();
        var handler = new JwtSecurityTokenHandler();

        var withSession = handler.ReadJwtToken(
            manager.IssueJwtAsync(User(), "relivewp.net", null, sessionId).Token);
        var withoutSession = handler.ReadJwtToken(
            manager.IssueJwtAsync(User(), "relivewp.net").Token);

        Assert.Equal(sessionId.ToString(), withSession.Claims.Single(c => c.Type == "sid").Value);
        Assert.DoesNotContain(withoutSession.Claims, c => c.Type == "sid");
    }

    [Fact]
    public async Task Replaying_a_rotated_refresh_token_revokes_the_whole_session()
    {
        var manager = Manager();
        var user = User();
        var sessionId = Guid.NewGuid();

        var first = await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, sessionId);
        var second = await manager.IssueRefreshTokenAsync(user, "sync.relivewp.net", null, sessionId);

        // simulate the first having already been redeemed and rotated away
        var stored = await _db.LiveRefreshTokens.SingleAsync(t => t.ServiceTarget == "relivewp.net");
        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        Assert.Null(await manager.RedeemRefreshTokenAsync(first.Token));

        var sibling = await _db.LiveRefreshTokens.AsNoTracking()
            .SingleAsync(t => t.ServiceTarget == "sync.relivewp.net");
        Assert.NotNull(sibling.RevokedAt);
        Assert.NotEqual(default, second.Expires);
    }

    [Fact]
    public async Task Revoking_a_session_leaves_other_sessions_alone()
    {
        var manager = Manager();
        var user = User();
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();

        await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, mine);
        await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, theirs);

        await manager.RevokeTokensForSessionAsync(mine);

        var tokens = await _db.LiveRefreshTokens.AsNoTracking().ToListAsync();
        Assert.NotNull(tokens.Single(t => t.SsoSessionId == mine).RevokedAt);
        Assert.Null(tokens.Single(t => t.SsoSessionId == theirs).RevokedAt);
    }

    private async Task<LiveUser> SignedInAsync(Guid sessionId, DateTimeOffset absoluteExpiry)
    {
        var user = User();
        _db.Users.Add(user);
        _db.LiveSsoSessions.Add(new LiveSsoSession()
        {
            Id = sessionId,
            UserId = user.Id,
            TokenHash = sessionId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = absoluteExpiry,
            AbsoluteExpiresAt = absoluteExpiry,
        });
        await _db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task Session_bound_refresh_token_rotates_while_its_session_lives()
    {
        var manager = Manager();
        var sessionId = Guid.NewGuid();
        var user = await SignedInAsync(sessionId, DateTimeOffset.UtcNow.AddDays(30));

        var issued = await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, sessionId);

        var redemption = await manager.RedeemRefreshTokenAsync(issued.Token);

        Assert.NotNull(redemption);
        Assert.Equal(sessionId, redemption!.SsoSessionId);
    }

    [Fact]
    public async Task Session_bound_refresh_token_stops_once_its_session_lapses()
    {
        var manager = Manager();
        var sessionId = Guid.NewGuid();
        var user = await SignedInAsync(sessionId, DateTimeOffset.UtcNow.AddHours(12));

        // the refresh token outlives a transient session, so the session is what has to end it
        var issued = await manager.IssueRefreshTokenAsync(user, "relivewp.net", TimeSpan.FromDays(14), sessionId);

        var session = await _db.LiveSsoSessions.SingleAsync(s => s.Id == sessionId);
        session.AbsoluteExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        Assert.Null(await manager.RedeemRefreshTokenAsync(issued.Token));
    }

    [Fact]
    public async Task Session_bound_refresh_token_stops_once_its_session_is_revoked()
    {
        var manager = Manager();
        var sessionId = Guid.NewGuid();
        var user = await SignedInAsync(sessionId, DateTimeOffset.UtcNow.AddDays(30));

        var issued = await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, sessionId);

        var session = await _db.LiveSsoSessions.SingleAsync(s => s.Id == sessionId);
        session.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        Assert.Null(await manager.RedeemRefreshTokenAsync(issued.Token));
    }

    [Fact]
    public async Task Replaying_a_rotated_refresh_token_revokes_its_session_too()
    {
        var manager = Manager();
        var sessionId = Guid.NewGuid();
        var user = await SignedInAsync(sessionId, DateTimeOffset.UtcNow.AddDays(30));

        var issued = await manager.IssueRefreshTokenAsync(user, "relivewp.net", null, sessionId);

        Assert.NotNull(await manager.RedeemRefreshTokenAsync(issued.Token));
        Assert.Null(await manager.RedeemRefreshTokenAsync(issued.Token));

        var session = await _db.LiveSsoSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId);
        Assert.NotNull(session.RevokedAt);
    }

    [Fact]
    public async Task Web_refresh_tokens_are_shorter_lived_than_device_ones()
    {
        var manager = Manager();
        var user = User();

        var device = await manager.IssueRefreshTokenAsync(user, "relivewp.net");
        var web = await manager.IssueRefreshTokenAsync(user, "sync.relivewp.net", manager.WebRefreshTokenLifetime);

        Assert.True(web.Expires < device.Expires);
    }
}
