using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Services;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class ConnectionTokenEncryptionTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly ConnectionSecretProtector protector = TestSecretProtector.CreateKeyed();

    public ConnectionTokenEncryptionTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task Tokens_are_stored_encrypted_and_read_back_as_plaintext()
    {
        var id = await SeedConnectionAsync("ya29.access", "1//refresh");

        var storedAccess = await ReadStoredAsync(id, "AccessToken");
        var storedRefresh = await ReadStoredAsync(id, "RefreshToken");
        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, storedAccess);
        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, storedRefresh);
        Assert.DoesNotContain("ya29.access", storedAccess);
        Assert.DoesNotContain("1//refresh", storedRefresh);

        var loaded = await LoadAsync(id);
        Assert.Equal("ya29.access", loaded.AccessToken);
        Assert.Equal("1//refresh", loaded.RefreshToken);
    }

    [Fact]
    public async Task Empty_tokens_stay_empty()
    {
        var id = await SeedConnectionAsync("", "");

        Assert.Equal("", await ReadStoredAsync(id, "AccessToken"));
        Assert.Equal("", await ReadStoredAsync(id, "RefreshToken"));
    }

    [Fact]
    public async Task Legacy_plaintext_tokens_still_read()
    {
        var id = await SeedLegacyConnectionAsync("ya29.access", "1//refresh");

        var loaded = await LoadAsync(id);
        Assert.Equal("ya29.access", loaded.AccessToken);
        Assert.Equal("1//refresh", loaded.RefreshToken);
    }

    [Fact]
    public async Task Backfill_encrypts_plaintext_rows_and_leaves_the_rest()
    {
        var legacy = await SeedLegacyConnectionAsync("ya29.legacy", "1//legacy");
        var partial = await SeedLegacyConnectionAsync("mastodon-token", "");
        var dav = await SeedLegacyConnectionAsync("", "");
        var current = await SeedConnectionAsync("ya29.current", "1//current");
        var currentAccess = await ReadStoredAsync(current, "AccessToken");

        using (var db = NewContext())
            Assert.Equal(2, db.EncryptPlaintextTokens());

        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, await ReadStoredAsync(legacy, "AccessToken"));
        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, await ReadStoredAsync(legacy, "RefreshToken"));
        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, await ReadStoredAsync(partial, "AccessToken"));
        Assert.Equal("", await ReadStoredAsync(partial, "RefreshToken"));
        Assert.Equal("", await ReadStoredAsync(dav, "AccessToken"));
        Assert.Equal(currentAccess, await ReadStoredAsync(current, "AccessToken"));

        var loaded = await LoadAsync(legacy);
        Assert.Equal("ya29.legacy", loaded.AccessToken);
        Assert.Equal("1//legacy", loaded.RefreshToken);

        using (var db = NewContext())
            Assert.Equal(0, db.EncryptPlaintextTokens());
    }

    [Fact]
    public async Task A_refresh_over_a_concurrent_write_keeps_its_tokens_encrypted()
    {
        var id = await SeedConnectionAsync("old-access", "old-refresh");

        using var refresher = NewContext();
        var service = await refresher.ConnectedServices.SingleAsync(s => s.Id == id);

        using (var other = NewContext())
        {
            var concurrent = await other.ConnectedServices.SingleAsync(s => s.Id == id);
            concurrent.EnabledCapabilities = ServiceCaps.Calendar;
            concurrent.RowVersion++;
            await other.SaveChangesAsync();
        }

        service.AccessToken = "new-access";
        await refresher.SaveChangesOverConcurrentWritesAsync();

        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, await ReadStoredAsync(id, "AccessToken"));
        Assert.StartsWith(ConnectionTokenConverter.ProtectedPrefix, await ReadStoredAsync(id, "RefreshToken"));

        var loaded = await LoadAsync(id);
        Assert.Equal("new-access", loaded.AccessToken);
        Assert.Equal("old-refresh", loaded.RefreshToken);
        Assert.Equal(ServiceCaps.Calendar, loaded.EnabledCapabilities);
    }

    private async Task<Guid> SeedConnectionAsync(string accessToken, string refreshToken)
    {
        using var db = NewContext();

        var service = new LiveConnectedService
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Service = WebDav.SERVICE_NAME,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Flags = LiveConnectedServiceFlags.None,
            AvailableCapabilities = ServiceCaps.Contacts | ServiceCaps.Calendar,
            EnabledCapabilities = ServiceCaps.Contacts,
            ServiceProfile = new LiveConnectedServiceProfile { UserId = "someone" },
        };

        db.ConnectedServices.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }

    private async Task<Guid> SeedLegacyConnectionAsync(string accessToken, string refreshToken)
    {
        var id = await SeedConnectionAsync("", "");

        using var db = NewContext();
        await db.Database.ExecuteSqlRawAsync(
            """UPDATE "ConnectedServices" SET "AccessToken" = @access, "RefreshToken" = @refresh WHERE "Id" = @id""",
            new SqliteParameter("@access", accessToken),
            new SqliteParameter("@refresh", refreshToken),
            new SqliteParameter("@id", StoredId(id)));

        return id;
    }

    private async Task<string> ReadStoredAsync(Guid id, string column)
    {
        using var db = NewContext();

        var stored = await db.Database
            .SqlQueryRaw<string>($"""SELECT "{column}" AS "Value" FROM "ConnectedServices" WHERE "Id" = @id""",
                new SqliteParameter("@id", StoredId(id)))
            .ToListAsync();

        return Assert.Single(stored);
    }

    private async Task<LiveConnectedService> LoadAsync(Guid id)
    {
        using var db = NewContext();
        return await db.ConnectedServices.AsNoTracking().SingleAsync(s => s.Id == id);
    }

    private static string StoredId(Guid id) => id.ToString().ToUpperInvariant();

    private ConnectedServicesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ConnectedServicesDbContext>().UseSqlite(connection).Options, protector);
}
