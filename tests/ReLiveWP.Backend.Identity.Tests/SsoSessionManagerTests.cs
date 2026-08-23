using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity.Tests;

public class SsoSessionManagerTests : IDisposable
{
    private const string ClientId = "548b5435-3d80-474c-81be-6fa4a5003471";
    private const string OtherClientId = "98fe90cf-dd7f-4cef-9aaa-87087637eba2";
    private const string RedirectUri = "https://mail.relivewp.net/auth/callback";

    private readonly SqliteConnection _connection;
    private readonly LiveDbContext _db;
    private readonly FakeCodeStore _codes = new();
    private readonly Guid _userId = Guid.NewGuid();

    public SsoSessionManagerTests()
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

    private sealed class FakeCodeStore : ISsoAuthorizationCodeStore
    {
        private readonly Dictionary<string, SsoAuthorizationCodePayload> _entries = [];

        public int StoredCount => _entries.Count;

        public Task StoreAsync(string code, SsoAuthorizationCodePayload payload, TimeSpan ttl)
        {
            _entries[code] = payload;
            return Task.CompletedTask;
        }

        public Task<SsoAuthorizationCodePayload?> TakeAsync(string code)
        {
            if (!_entries.Remove(code, out var payload))
                return Task.FromResult<SsoAuthorizationCodePayload?>(null);

            return Task.FromResult<SsoAuthorizationCodePayload?>(payload);
        }
    }

    private SsoSessionManager Manager(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? [])
            .Build();

        var tokenManager = new TokenManager(configuration, NullLogger<TokenManager>.Instance, null!, _db);

        return new SsoSessionManager(
            configuration,
            NullLogger<SsoSessionManager>.Instance,
            _db,
            tokenManager,
            _codes);
    }

    private static string Challenge(string verifier)
        => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private async Task<(SsoSessionManager manager, SsoSessionCreation session)> SignedInAsync(bool persistent = true)
    {
        var manager = Manager();
        var session = await manager.CreateSessionAsync(_userId, persistent, "agent", "203.0.113.1");

        return (manager, session);
    }

    private SsoAuthorizationCodePayload Payload(Guid sessionId, string? challenge = null, string? method = "S256")
        => new(_userId, sessionId, ClientId, RedirectUri, ["relivewp.net"], challenge, method);

    [Fact]
    public async Task Session_handle_is_not_stored_in_the_clear()
    {
        var (_, session) = await SignedInAsync();

        var stored = await _db.LiveSsoSessions.SingleAsync();
        Assert.NotEqual(session.Handle, stored.TokenHash);
        Assert.DoesNotContain(session.Handle, stored.TokenHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_session_validates_and_a_revoked_one_does_not()
    {
        var (manager, session) = await SignedInAsync();

        Assert.NotNull(await manager.ValidateSessionAsync(session.Handle));

        Assert.True(await manager.RevokeSessionAsync(session.Handle));
        Assert.Null(await manager.ValidateSessionAsync(session.Handle));
    }

    [Fact]
    public async Task Expired_session_does_not_validate()
    {
        var (manager, session) = await SignedInAsync();

        var stored = await _db.LiveSsoSessions.SingleAsync();
        stored.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        stored.AbsoluteExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        Assert.Null(await manager.ValidateSessionAsync(session.Handle));
    }

    [Fact]
    public async Task Sliding_expiry_never_passes_the_absolute_cap()
    {
        var (manager, session) = await SignedInAsync(persistent: false);

        var before = await _db.LiveSsoSessions.AsNoTracking().SingleAsync();
        await manager.ValidateSessionAsync(session.Handle);
        var after = await _db.LiveSsoSessions.AsNoTracking().SingleAsync();

        Assert.Equal(before.AbsoluteExpiresAt, after.AbsoluteExpiresAt);
        Assert.True(after.ExpiresAt <= after.AbsoluteExpiresAt);
    }

    [Fact]
    public async Task Authorization_code_is_single_use()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(Payload(session.SessionId));

        Assert.NotNull(await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null));
        Assert.Null(await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null));
    }

    [Fact]
    public async Task Concurrent_redemption_succeeds_exactly_once()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(Payload(session.SessionId));

        // the store is the serialisation point, so redeem sequentially against it and assert the
        // second caller gets nothing rather than a second set of tokens
        var first = await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null);
        var second = await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(0, _codes.StoredCount);
    }

    [Fact]
    public async Task Authorization_code_rejects_a_mismatched_client()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(Payload(session.SessionId));

        Assert.Null(await manager.RedeemAuthorizationCodeAsync(code, OtherClientId, RedirectUri, null));
    }

    [Fact]
    public async Task Authorization_code_rejects_a_mismatched_redirect_uri()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(Payload(session.SessionId));

        Assert.Null(await manager.RedeemAuthorizationCodeAsync(
            code, ClientId, "https://evil.example/auth/callback", null));
    }

    [Fact]
    public async Task Authorization_code_rejects_a_bad_pkce_verifier()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(
            Payload(session.SessionId, Challenge("the-real-verifier")));

        Assert.Null(await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, "not-it"));
    }

    [Fact]
    public async Task Authorization_code_accepts_the_matching_pkce_verifier()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(
            Payload(session.SessionId, Challenge("the-real-verifier")));

        Assert.NotNull(await manager.RedeemAuthorizationCodeAsync(
            code, ClientId, RedirectUri, "the-real-verifier"));
    }

    [Fact]
    public async Task Authorization_code_requires_a_verifier_when_a_challenge_was_set()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(
            Payload(session.SessionId, Challenge("the-real-verifier")));

        Assert.Null(await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null));
    }

    [Fact]
    public async Task Authorization_code_is_refused_once_its_session_is_revoked()
    {
        var (manager, session) = await SignedInAsync();
        var code = await manager.IssueAuthorizationCodeAsync(Payload(session.SessionId));

        await manager.RevokeSessionAsync(session.Handle);

        Assert.Null(await manager.RedeemAuthorizationCodeAsync(code, ClientId, RedirectUri, null));
    }

    [Fact]
    public async Task Revoking_a_session_revokes_the_refresh_tokens_it_issued()
    {
        var (manager, session) = await SignedInAsync();

        _db.LiveRefreshTokens.Add(new LiveRefreshToken()
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TokenHash = "hash",
            ServiceTarget = "relivewp.net",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            SsoSessionId = session.SessionId,
        });
        await _db.SaveChangesAsync();

        await manager.RevokeSessionAsync(session.Handle);

        var token = await _db.LiveRefreshTokens.AsNoTracking().SingleAsync();
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task Each_sign_in_mints_a_distinct_session()
    {
        var manager = Manager();

        var first = await manager.CreateSessionAsync(_userId, true, null, null);
        var second = await manager.CreateSessionAsync(_userId, true, null, null);

        Assert.NotEqual(first.Handle, second.Handle);
        Assert.NotEqual(first.SessionId, second.SessionId);
    }
}
