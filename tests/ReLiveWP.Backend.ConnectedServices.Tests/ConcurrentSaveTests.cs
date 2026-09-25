using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

// sqlite has no xmin, so the other writer bumps RowVersion by hand to stand in for postgres doing it
public class ConcurrentSaveTests : IDisposable
{
    private readonly SqliteConnection connection;

    public ConcurrentSaveTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task A_plain_save_over_a_concurrent_write_throws()
    {
        var id = await SeedConnectionAsync();

        using var refresher = NewContext();
        var service = await refresher.ConnectedServices.SingleAsync(s => s.Id == id);

        await ChangeCapabilitiesElsewhereAsync(id, ServiceCaps.Calendar);

        service.AccessToken = "new-access";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => refresher.SaveChangesAsync());
    }

    [Fact]
    public async Task A_refresh_keeps_its_tokens_and_the_concurrent_capability_change()
    {
        var id = await SeedConnectionAsync();

        using var refresher = NewContext();
        var service = await refresher.ConnectedServices.SingleAsync(s => s.Id == id);

        await ChangeCapabilitiesElsewhereAsync(id, ServiceCaps.Calendar);

        service.AccessToken = "new-access";
        service.RefreshToken = "new-refresh";
        await refresher.SaveChangesOverConcurrentWritesAsync();

        var saved = await LoadAsync(id);
        Assert.Equal("new-access", saved.AccessToken);
        Assert.Equal("new-refresh", saved.RefreshToken);
        Assert.Equal(ServiceCaps.Calendar, saved.EnabledCapabilities);
    }

    [Fact]
    public async Task A_refreshed_profile_survives_a_concurrent_write()
    {
        var id = await SeedConnectionAsync();

        using var refresher = NewContext();
        var service = await refresher.ConnectedServices.SingleAsync(s => s.Id == id);

        await ChangeCapabilitiesElsewhereAsync(id, ServiceCaps.Calendar);

        service.AccessToken = "new-access";
        service.ServiceProfile.DisplayName = "Renamed";
        await refresher.SaveChangesOverConcurrentWritesAsync();

        var saved = await LoadAsync(id);
        Assert.Equal("new-access", saved.AccessToken);
        Assert.Equal("Renamed", saved.ServiceProfile.DisplayName);
        Assert.Equal(ServiceCaps.Calendar, saved.EnabledCapabilities);
    }

    [Fact]
    public async Task A_relink_can_replace_the_whole_profile_on_a_tracked_connection()
    {
        var id = await SeedConnectionAsync();

        using var relinker = NewContext();
        var service = await relinker.ConnectedServices.SingleAsync(s => s.Id == id);

        service.ServiceProfile = new LiveConnectedServiceProfile
        {
            UserId = "wam@dav.example.com",
            Username = "wam",
            Label = "wam@dav.example.com/dav",
        };
        await relinker.SaveChangesOverConcurrentWritesAsync();

        var saved = await LoadAsync(id);
        Assert.Equal("wam@dav.example.com", saved.ServiceProfile.UserId);
        Assert.Equal("wam@dav.example.com/dav", saved.ServiceProfile.Label);
        Assert.Null(saved.ServiceProfile.DisplayName);
    }

    [Fact]
    public async Task A_relink_replacing_the_profile_survives_a_concurrent_write()
    {
        var id = await SeedConnectionAsync();

        using var relinker = NewContext();
        var service = await relinker.ConnectedServices.SingleAsync(s => s.Id == id);

        await ChangeCapabilitiesElsewhereAsync(id, ServiceCaps.Calendar);

        service.AccessToken = "new-access";
        service.ServiceProfile = new LiveConnectedServiceProfile
        {
            UserId = "wam@dav.example.com",
            Username = "wam",
            Label = "wam@dav.example.com/dav",
        };
        await relinker.SaveChangesOverConcurrentWritesAsync();

        var saved = await LoadAsync(id);
        Assert.Equal("new-access", saved.AccessToken);
        Assert.Equal("wam@dav.example.com", saved.ServiceProfile.UserId);
        Assert.Equal("wam@dav.example.com/dav", saved.ServiceProfile.Label);
        Assert.Null(saved.ServiceProfile.DisplayName);
        Assert.Equal(ServiceCaps.Calendar, saved.EnabledCapabilities);
    }

    [Fact]
    public async Task A_refresh_against_a_deleted_connection_does_not_throw()
    {
        var id = await SeedConnectionAsync();

        using var refresher = NewContext();
        var service = await refresher.ConnectedServices.SingleAsync(s => s.Id == id);

        using (var deleter = NewContext())
        {
            deleter.ConnectedServices.Remove(await deleter.ConnectedServices.SingleAsync(s => s.Id == id));
            await deleter.SaveChangesAsync();
        }

        service.AccessToken = "new-access";
        await refresher.SaveChangesOverConcurrentWritesAsync();

        using var check = NewContext();
        Assert.False(await check.ConnectedServices.AnyAsync(s => s.Id == id));
    }

    private async Task<Guid> SeedConnectionAsync()
    {
        using var db = NewContext();

        var service = new LiveConnectedService
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Service = WebDav.SERVICE_NAME,
            AccessToken = "old-access",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow,
            Flags = LiveConnectedServiceFlags.None,
            AvailableCapabilities = ServiceCaps.Contacts | ServiceCaps.Calendar,
            EnabledCapabilities = ServiceCaps.Contacts,
            ServiceProfile = new LiveConnectedServiceProfile { UserId = "old", DisplayName = "Original" },
        };

        db.ConnectedServices.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }

    private async Task ChangeCapabilitiesElsewhereAsync(Guid id, ServiceCaps enabled)
    {
        using var other = NewContext();
        var service = await other.ConnectedServices.SingleAsync(s => s.Id == id);

        service.EnabledCapabilities = enabled;
        service.RowVersion++;
        await other.SaveChangesAsync();
    }

    private async Task<LiveConnectedService> LoadAsync(Guid id)
    {
        using var db = NewContext();
        return await db.ConnectedServices.AsNoTracking().SingleAsync(s => s.Id == id);
    }

    private ConnectedServicesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ConnectedServicesDbContext>().UseSqlite(connection).Options);
}
