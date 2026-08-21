using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Grpc;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Services.Grpc;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;
using GoogleService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Google;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class SharedCapabilitiesTests : IDisposable
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string Did = "did:plc:amyamyamyamyamyamyamy";

    private readonly SqliteConnection connection;

    public SharedCapabilitiesTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task A_capability_the_provider_cannot_serve_publicly_cannot_be_shared()
    {
        using var db = NewContext();
        var id = AddConnection(db, Owner, AtProto.SERVICE_NAME,
            enabled: ServiceCaps.SocialFeed | ServiceCaps.SocialPost);
        await db.SaveChangesAsync();

        await UpdateAsync(db, id, enabled: ServiceCaps.SocialFeed | ServiceCaps.SocialPost,
            shared: ServiceCaps.SocialFeed | ServiceCaps.SocialPost);

        Assert.Equal(ServiceCaps.SocialFeed, (await db.ConnectedServices.SingleAsync()).SharedCapabilities);
    }

    [Fact]
    public async Task A_capability_that_is_not_enabled_cannot_be_shared()
    {
        using var db = NewContext();
        var id = AddConnection(db, Owner, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        await UpdateAsync(db, id, enabled: ServiceCaps.SocialFeed,
            shared: ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos);

        Assert.Equal(ServiceCaps.SocialFeed, (await db.ConnectedServices.SingleAsync()).SharedCapabilities);
    }

    [Fact]
    public async Task Turning_a_capability_off_stops_sharing_it()
    {
        using var db = NewContext();
        var id = AddConnection(db, Owner, AtProto.SERVICE_NAME,
            enabled: ServiceCaps.SocialFeed, shared: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        await UpdateAsync(db, id, enabled: ServiceCaps.None);

        Assert.Equal(ServiceCaps.None, (await db.ConnectedServices.SingleAsync()).SharedCapabilities);
    }

    // every capability toggle in the web ui patches enabled_capabilities alone, so an absent field
    // must not read as zero
    [Fact]
    public async Task A_patch_that_omits_sharing_leaves_it_alone()
    {
        using var db = NewContext();
        var id = AddConnection(db, Owner, AtProto.SERVICE_NAME,
            enabled: ServiceCaps.SocialFeed | ServiceCaps.SocialPost, shared: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        await UpdateAsync(db, id, enabled: ServiceCaps.SocialFeed | ServiceCaps.SocialPost);

        Assert.Equal(ServiceCaps.SocialFeed, (await db.ConnectedServices.SingleAsync()).SharedCapabilities);
    }

    // the phone has one document store and one camera roll, so these cannot be split across accounts:
    // SkyDocs and SkyDrive merge every enabled provider into a single tree
    [Theory]
    [InlineData(ServiceCaps.FileStorage)]
    [InlineData(ServiceCaps.PhotoSync)]
    public async Task Enabling_a_single_service_capability_turns_it_off_on_the_others(ServiceCaps cap)
    {
        using var db = NewContext();
        var onedrive = AddConnection(db, Owner, MicrosoftService.SERVICE_NAME, enabled: cap);
        var share = AddConnection(db, Owner, WebDav.SERVICE_NAME, enabled: ServiceCaps.None);
        await db.SaveChangesAsync();

        await UpdateAsync(db, share, enabled: cap);

        Assert.Equal(cap, (await db.ConnectedServices.SingleAsync(c => c.Id == share)).EnabledCapabilities);
        Assert.Equal(ServiceCaps.None, (await db.ConnectedServices.SingleAsync(c => c.Id == onedrive)).EnabledCapabilities);
    }

    [Fact]
    public async Task A_single_service_capability_does_not_disturb_another_users_connection()
    {
        using var db = NewContext();
        var theirs = AddConnection(db, Other, MicrosoftService.SERVICE_NAME, enabled: ServiceCaps.FileStorage);
        var mine = AddConnection(db, Owner, WebDav.SERVICE_NAME, enabled: ServiceCaps.None);
        await db.SaveChangesAsync();

        await UpdateAsync(db, mine, enabled: ServiceCaps.FileStorage);

        Assert.Equal(ServiceCaps.FileStorage, (await db.ConnectedServices.SingleAsync(c => c.Id == theirs)).EnabledCapabilities);
    }

    [Fact]
    public async Task Sharing_survives_a_round_trip_through_get_connections()
    {
        using var db = NewContext();
        var id = AddConnection(db, Owner, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        await UpdateAsync(db, id, enabled: ServiceCaps.SocialFeed, shared: ServiceCaps.SocialFeed);

        var writer = new ListStreamWriter<Connection>();
        await NewService(db).GetConnections(new ConnectionsRequest(), writer, new StubCallContext(Owner));

        Assert.Equal((uint)ServiceCaps.SocialFeed, writer.Items.Single().SharedCapabilities);
    }

    // adding any field to this message is a decision, not an accident: an id here plus a forged
    // X-User-Id would be half a proxy credential
    [Fact]
    public void A_shared_connection_carries_nothing_that_identifies_the_connection()
    {
        var fields = SharedConnection.Descriptor.Fields.InDeclarationOrder().Select(f => f.Name).ToHashSet();

        Assert.Equal(
            ["ownerUserId", "service", "userId", "userName", "displayName", "avatarUrl", "sharedCapabilities"],
            fields);
    }

    [Fact]
    public async Task Only_shared_healthy_connections_are_returned()
    {
        using var db = NewContext();
        AddConnection(db, Owner, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed, shared: ServiceCaps.SocialFeed);
        AddConnection(db, Owner, GoogleService.SERVICE_NAME, enabled: ServiceCaps.PhotoSync);
        AddConnection(db, Other, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed,
            shared: ServiceCaps.SocialFeed, flags: LiveConnectedServiceFlags.Busted);
        await db.SaveChangesAsync();

        var response = await SharedAsync(db, [Owner, Other]);

        var only = Assert.Single(response.Connections);
        Assert.Equal(Owner.ToString(), only.OwnerUserId);
        Assert.Equal(AtProto.SERVICE_NAME, only.Service);
        Assert.Equal(Did, only.UserId);
    }

    [Fact]
    public async Task A_requested_capability_narrows_the_result()
    {
        using var db = NewContext();
        AddConnection(db, Owner, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed, shared: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        Assert.Single((await SharedAsync(db, [Owner], ServiceCaps.SocialFeed)).Connections);
        Assert.Empty((await SharedAsync(db, [Owner], ServiceCaps.SocialPhotos)).Connections);
    }

    [Fact]
    public async Task Unknown_and_unparseable_users_return_nothing()
    {
        using var db = NewContext();
        AddConnection(db, Owner, AtProto.SERVICE_NAME, enabled: ServiceCaps.SocialFeed, shared: ServiceCaps.SocialFeed);
        await db.SaveChangesAsync();

        var request = new SharedConnectionsRequest { UserIds = { Other.ToString(), "not-a-guid" } };
        var response = await NewService(db).GetSharedConnections(request, new StubCallContext(Owner));

        Assert.Empty(response.Connections);
    }

    [Fact]
    public async Task An_oversized_lookup_is_rejected()
    {
        using var db = NewContext();

        var request = new SharedConnectionsRequest();
        for (var i = 0; i < 51; i++)
            request.UserIds.Add(Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => NewService(db).GetSharedConnections(request, new StubCallContext(Owner)));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    private Task<SharedConnectionsResponse> SharedAsync(
        ConnectedServicesDbContext db, Guid[] users, ServiceCaps? capabilities = null)
    {
        var request = new SharedConnectionsRequest();
        request.UserIds.AddRange(users.Select(u => u.ToString()));

        if (capabilities is { } caps)
            request.Capabilities = (uint)caps;

        return NewService(db).GetSharedConnections(request, new StubCallContext(Owner));
    }

    private Task UpdateAsync(ConnectedServicesDbContext db, Guid connectionId, ServiceCaps enabled, ServiceCaps? shared = null)
    {
        var request = new UpdateCapabilitiesRequest
        {
            ConnectionId = connectionId.ToString(),
            Capabilities = (uint)enabled,
        };

        if (shared is { } value)
            request.SharedCapabilities = (uint)value;

        return NewService(db).UpdateCapabilities(request, new StubCallContext(Owner));
    }

    private static Guid AddConnection(
        ConnectedServicesDbContext db,
        Guid userId,
        string service,
        ServiceCaps enabled,
        ServiceCaps shared = ServiceCaps.None,
        LiveConnectedServiceFlags flags = LiveConnectedServiceFlags.None)
    {
        var id = Guid.NewGuid();

        db.ConnectedServices.Add(new LiveConnectedService
        {
            Id = id,
            UserId = userId,
            Service = service,
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Flags = flags,
            AvailableCapabilities = Descriptions()[service].ServiceCapabilities,
            EnabledCapabilities = enabled,
            SharedCapabilities = shared,
            ServiceProfile = new LiveConnectedServiceProfile { UserId = Did, Username = "amyy.me" },
        });

        return id;
    }

    private ConnectedAccountsService NewService(ConnectedServicesDbContext db) =>
        new(null!, Descriptions(), null!, db, null!, NullLogger<ConnectedAccountsService>.Instance);

    private static ConnectedServicesContainer Descriptions() => new()
    {
        [AtProto.SERVICE_NAME] = new ConnectedServiceDescription
        {
            ServiceId = AtProto.SERVICE_NAME,
            DisplayName = "AtProto",
            ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialCheckIn
                | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost | ServiceCaps.SocialPhotos,
            ShareableCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos,
        },
        [GoogleService.SERVICE_NAME] = new ConnectedServiceDescription
        {
            ServiceId = GoogleService.SERVICE_NAME,
            DisplayName = "Google",
            ServiceCapabilities = ServiceCaps.PhotoSync,
        },
        [MicrosoftService.SERVICE_NAME] = new ConnectedServiceDescription
        {
            ServiceId = MicrosoftService.SERVICE_NAME,
            DisplayName = "Microsoft",
            ServiceCapabilities = ServiceCaps.FileStorage | ServiceCaps.PhotoSync,
        },
        [WebDav.SERVICE_NAME] = new ConnectedServiceDescription
        {
            ServiceId = WebDav.SERVICE_NAME,
            DisplayName = "WebDAV",
            LinkMode = ServiceLinkMode.Credentials,
            ServiceCapabilities = ServiceCaps.FileStorage | ServiceCaps.PhotoSync,
        },
    };

    private ConnectedServicesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ConnectedServicesDbContext>().UseSqlite(connection).Options);

    private sealed class ListStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Items { get; } = [];

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            Items.Add(message);
            return Task.CompletedTask;
        }
    }
}
