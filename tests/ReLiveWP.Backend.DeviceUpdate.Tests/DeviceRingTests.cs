using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class DeviceRingResolverTests
{
    private static readonly long[] Marker = [900000000];

    private static DeviceRing Resolve(long[] installed, long[] cached) =>
        DeviceRingResolver.Resolve(Marker, installed.ToHashSet(), cached.ToHashSet());

    [Fact]
    public void ReportingTheMarkerInstalledIsAReLiveWpDevice() =>
        Assert.Equal(DeviceRing.ReLiveWP, Resolve([900000000], []));

    // A detectoid the client evaluated as false is cached rather than reported, which is the only
    // positive evidence we ever get that a device is stock.
    [Fact]
    public void CachingTheMarkerWithoutReportingItIsAStockDevice() =>
        Assert.Equal(DeviceRing.Stock, Resolve([], [900000000]));

    [Fact]
    public void NeverHavingSeenTheMarkerIsUnknown() =>
        Assert.Equal(DeviceRing.Unknown, Resolve([], []));

    [Fact]
    public void ReportingItInstalledWinsOverAlsoHavingItCached() =>
        Assert.Equal(DeviceRing.ReLiveWP, Resolve([900000000], [900000000]));

    // Once a second revision of the marker ships, a device may still be reporting the first.
    [Fact]
    public void AnyRevisionOfTheMarkerCounts() =>
        Assert.Equal(DeviceRing.ReLiveWP,
            DeviceRingResolver.Resolve([900000000, 900000007], new HashSet<long> { 900000007 }, new HashSet<long>()));

    // Policy is looked up under the ReLiveWP ring unless the device has positively proven itself
    // stock, so an unanswered device cannot fall through to the unblocked rows.
    [Theory]
    [InlineData(DeviceRing.ReLiveWP, DeviceRing.ReLiveWP)]
    [InlineData(DeviceRing.Unknown, DeviceRing.ReLiveWP)]
    [InlineData(DeviceRing.Stock, DeviceRing.Stock)]
    public void OnlyAConfirmedStockDeviceGetsStockPolicy(DeviceRing resolved, DeviceRing policy) =>
        Assert.Equal(policy, resolved.PolicyRing());
}

public class DeviceRingServingTests : IAsyncLifetime
{
    private static readonly Guid Category = new("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8");

    private const long CategoryRevision = 10;
    private const long MarkerRevision = 900000000;

    private SqliteConnection connection = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        db.Updates.Add(Revision(CategoryRevision, Category, UpdateType.Category, isLeaf: false));
        db.Updates.Add(Revision(MarkerRevision, DeviceRingResolver.MarkerUpdateId, UpdateType.Detectoid, isLeaf: false));

        db.Prerequisites.Add(new UpdatePrerequisite
        {
            UpdateRevisionId = MarkerRevision,
            PrerequisiteUpdateId = Category,
            GroupId = 1,
            IsCategory = true
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        connection.Dispose();
        return Task.CompletedTask;
    }

    private static Update Revision(long revisionId, Guid updateId, UpdateType type, bool isLeaf) =>
        new()
        {
            RevisionId = revisionId,
            UpdateId = updateId,
            RevisionNumber = 1,
            UpdateType = type,
            Origin = revisionId >= 900000000 ? UpdateOrigin.Authored : UpdateOrigin.Crawled,
            IsLeafReported = isLeaf,
            IsLeaf = isLeaf,
            DeploymentAction = "Evaluate",
            Metadata = new UpdateMetadata { SyncXml = $"<UpdateIdentity UpdateID=\"{updateId}\" RevisionNumber=\"1\" />" }
        };

    private UpdatesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<UpdatesDbContext>().UseSqlite(connection).Options);

    private static string Request(long[] installed, long[] cached)
    {
        var installedIds = string.Concat(installed.Select(i => $"<int>{i}</int>"));
        var cachedIds = string.Concat(cached.Select(i => $"<int>{i}</int>"));

        return $"""
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <SyncUpdates xmlns="http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService">
                  <parameters>
                    <InstalledNonLeafUpdateIDs>{installedIds}</InstalledNonLeafUpdateIDs>
                    <OtherCachedUpdateIDs>{cachedIds}</OtherCachedUpdateIDs>
                  </parameters>
                </SyncUpdates>
              </s:Body>
            </s:Envelope>
            """;
    }

    private async Task<SyncUpdatesResult> SyncAsync(long[] installed, long[] cached)
    {
        using var db = NewContext();
        return await new UpdateService(db).SyncUpdatesAsync(Request(installed, cached));
    }

    // The marker sits behind the WP category, so a device that has reported nothing gets the
    // category and only reaches the marker on a later round. The ring is therefore Unknown for the
    // first couple of rounds of every session, which is exactly what the fail-closed default is for.
    [Fact]
    public async Task AFreshDeviceIsUnknownAndGetsTheCategoryFirst()
    {
        var first = await SyncAsync([], []);
        var offered = CaptureFixture.UpdateInfoIds(first.Xml, "NewUpdates");

        Assert.Equal(DeviceRing.Unknown, first.Ring);
        Assert.Contains(CategoryRevision, offered);
        Assert.DoesNotContain(MarkerRevision, offered);
    }

    [Fact]
    public async Task ADeviceThatReportsTheMarkerResolvesReLiveWp()
    {
        var result = await SyncAsync([CategoryRevision, MarkerRevision], []);

        Assert.Equal(DeviceRing.ReLiveWP, result.Ring);
    }

    [Fact]
    public async Task ADeviceThatEvaluatedTheMarkerFalseResolvesStock()
    {
        var result = await SyncAsync([CategoryRevision], [MarkerRevision]);

        Assert.Equal(DeviceRing.Stock, result.Ring);
    }

    // The marker's own prerequisite is the WP category, so a device converges within one round of
    // caching the taxonomy rather than needing the shim to be detected first.
    [Fact]
    public async Task TheMarkerIsOfferedOnceTheCategoryIsReported()
    {
        var result = await SyncAsync([CategoryRevision], []);

        Assert.Contains(MarkerRevision, CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates"));
    }
}
