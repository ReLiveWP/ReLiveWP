using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// A synthetic stand-in for the real 7.8 shape: a deployable Install update bundling a rollup, which
// in turn bundles the payload that declares the OS version it delivers.
public class DeploymentOverrideTests : IAsyncLifetime
{
    private static readonly Guid Category = new("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8");
    private static readonly Guid Deployable = new("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid Rollup = new("aaaaaaaa-0000-0000-0000-00000000000b");
    private static readonly Guid Payload = new("aaaaaaaa-0000-0000-0000-00000000000c");
    private static readonly Guid Unrelated = new("aaaaaaaa-0000-0000-0000-00000000000d");

    private const long CategoryRevision = 10;
    private const long MarkerRevision = 900000000;
    private const long DeployableRevision = 200;
    private const long RollupRevision = 201;
    private const long PayloadRevision = 202;
    private const long UnrelatedRevision = 203;

    private SqliteConnection connection = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        db.Updates.Add(Revision(CategoryRevision, Category, UpdateType.Category, "Evaluate", isLeaf: false));
        db.Updates.Add(Revision(MarkerRevision, DeviceRingResolver.MarkerUpdateId, UpdateType.Detectoid, "Evaluate", isLeaf: false));

        db.Updates.Add(Revision(DeployableRevision, Deployable, UpdateType.Software, "Install", isLeaf: true));
        db.Updates.Add(Revision(RollupRevision, Rollup, UpdateType.Software, "Bundle", isLeaf: true));
        db.Updates.Add(Revision(PayloadRevision, Payload, UpdateType.Software, "Bundle", isLeaf: true,
            deliveredOsVersion: "7.10.8858.136"));

        db.Updates.Add(Revision(UnrelatedRevision, Unrelated, UpdateType.Software, "Install", isLeaf: true,
            deliveredOsVersion: "7.10.8783.12"));

        db.Bundles.Add(new UpdateBundle { BundleRevisionId = DeployableRevision, BundledUpdateId = Rollup });
        db.Bundles.Add(new UpdateBundle { BundleRevisionId = RollupRevision, BundledUpdateId = Payload });

        foreach (var revision in (long[])[MarkerRevision, DeployableRevision, UnrelatedRevision])
            db.Prerequisites.Add(new UpdatePrerequisite
            {
                UpdateRevisionId = revision,
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

    private static Update Revision(
        long revisionId, Guid updateId, UpdateType type, string action, bool isLeaf, string? deliveredOsVersion = null)
    {
        var rules = deliveredOsVersion is null
            ? ""
            : "<ApplicabilityRules><IsInstalled><CspQuery LocUri=\"./DevDetail/SwV\" "
              + $"Comparison=\"GreaterThanOrEqualTo\" Value=\"{deliveredOsVersion}\" "
              + "xmlns=\"http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules\" /></IsInstalled></ApplicabilityRules>";

        return new Update
        {
            RevisionId = revisionId,
            UpdateId = updateId,
            RevisionNumber = 1,
            UpdateType = type,
            IsLeafReported = isLeaf,
            IsLeaf = isLeaf,
            DeploymentAction = action,
            LastChangeTime = CrawledOn,
            Metadata = new UpdateMetadata
            {
                SyncXml = $"<UpdateIdentity UpdateID=\"{updateId}\" RevisionNumber=\"1\" />{rules}"
            }
        };
    }

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

    // A crawled 7.8 update really does carry a 2013 stamp, which is the whole problem: it cannot be
    // left in place on a revision whose action we just changed.
    private static readonly DateTime CrawledOn = new(2013, 3, 14);
    private static readonly DateTime BlockedOn = new(2026, 9, 14);

    private async Task ApplyBlockAsync()
    {
        using var db = NewContext();
        var derived = await BlockSetDeriver.DeriveAsync(db, new Version("7.10.8858.136"));

        foreach (var updateId in derived.DeployableUpdateIds)
            db.Deployments.Add(new UpdateDeployment
            {
                Ring = DeviceRing.ReLiveWP,
                UpdateId = updateId,
                Action = UpdateService.BlockAction,
                LastChangeTime = BlockedOn,
            });

        await db.SaveChangesAsync();
    }

    private async Task<SyncUpdatesResult> SyncAsync(long[] installed, long[] cached)
    {
        using var db = NewContext();
        return await new UpdateService(db).SyncUpdatesAsync(Request(installed, cached));
    }

    private async Task<string?> ActionForAsync(long[] installed, long[] cached)
    {
        using var db = NewContext();
        var result = await new UpdateService(db).SyncUpdatesAsync(Request(installed, cached));

        return CaptureFixture.DeploymentActionFor(result.Xml, DeployableRevision);
    }

    [Fact]
    public async Task DerivesTheDeployableRootThroughTwoBundleHops()
    {
        using var db = NewContext();
        var derived = await BlockSetDeriver.DeriveAsync(db, new Version("7.10.8858.136"));

        Assert.Equal([Deployable], derived.DeployableUpdateIds);
        Assert.Equal([PayloadRevision], derived.PayloadRevisions);
        Assert.Equal([RollupRevision], derived.BundleRevisions);
    }

    // The unrelated update delivers 7.10.8783.12, which is lower even though it sorts higher as a
    // string than some 8858 builds would.
    [Fact]
    public async Task LeavesUpdatesBelowTheThresholdAlone()
    {
        using var db = NewContext();
        var derived = await BlockSetDeriver.DeriveAsync(db, new Version("7.10.8858.136"));

        Assert.DoesNotContain(Unrelated, derived.DeployableUpdateIds);
    }

    [Fact]
    public async Task AConfirmedStockDeviceIsStillOfferedTheInstall()
    {
        await ApplyBlockAsync();

        Assert.Equal("Install", await ActionForAsync([CategoryRevision], [MarkerRevision]));
    }

    // Withheld, not relabelled. A softer action still leaves the client holding metadata it
    // evaluates as installable, and it goes on to pull the bundle children and ask for their file
    // locations, which is exactly what a real phone was observed doing.
    [Fact]
    public async Task ADeviceCarryingTheShimIsNotSentTheRevisionAtAll()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision, MarkerRevision], []);
        var offered = CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates");

        Assert.DoesNotContain(DeployableRevision, offered);
        Assert.Null(await ActionForAsync([CategoryRevision, MarkerRevision], []));
    }

    // The payloads are the thing that actually carries 7.8 onto the phone, and they only ride along
    // because a parent bundles them. Dropping the parent has to drop them too.
    [Fact]
    public async Task TheBundledPayloadsGoWithTheBlockedParent()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision, MarkerRevision], []);
        var offered = CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates");

        Assert.DoesNotContain(RollupRevision, offered);
        Assert.DoesNotContain(PayloadRevision, offered);
    }

    [Fact]
    public async Task ADeviceThatHasNotAnsweredYetIsNotSentTheRevision()
    {
        await ApplyBlockAsync();

        Assert.Null(await ActionForAsync([CategoryRevision], []));
    }

    // The case that actually matters in the field: a phone that already cached 7.8 before the shim
    // went on. Withholding alone leaves it holding what it already has, so it has to be told to
    // drop it.
    [Fact]
    public async Task AnAlreadyCachedBlockedRevisionIsEvicted()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision, MarkerRevision], [DeployableRevision, RollupRevision, PayloadRevision]);
        var evicted = CaptureFixture.OutOfScopeIds(result.Xml);

        Assert.Contains(DeployableRevision, evicted);
        Assert.Contains(RollupRevision, evicted);
        Assert.Contains(PayloadRevision, evicted);

        Assert.DoesNotContain(DeployableRevision, CaptureFixture.UpdateInfoIds(result.Xml, "ChangedUpdates"));
        Assert.DoesNotContain(DeployableRevision, CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates"));
    }

    // Upstream never sent the element at all, so a stock device must not start seeing it now.
    [Fact]
    public async Task AStockDeviceIsNeverSentAnEvictionList()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision], [MarkerRevision, DeployableRevision]);

        Assert.Empty(CaptureFixture.OutOfScopeIds(result.Xml));
        Assert.DoesNotContain("OutOfScopeRevisionIDs", result.Xml);
    }

    // Only what the client says it holds. Naming a revision it never had is noise at best.
    [Fact]
    public async Task EvictionOnlyNamesRevisionsTheClientReportedCaching()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision, MarkerRevision], [DeployableRevision]);
        var evicted = CaptureFixture.OutOfScopeIds(result.Xml);

        Assert.Equal([DeployableRevision], evicted.Order().ToList());
    }

    [Fact]
    public async Task WithNoBlockRowsNothingChanges()
    {
        Assert.Equal("Install", await ActionForAsync([CategoryRevision, MarkerRevision], []));
        Assert.Empty(CaptureFixture.OutOfScopeIds((await SyncAsync([CategoryRevision, MarkerRevision], [])).Xml));
    }

    [Fact]
    public async Task AnUnblockedDeploymentKeepsTheCrawledDate()
    {
        await ApplyBlockAsync();

        var result = await SyncAsync([CategoryRevision], [MarkerRevision]);

        Assert.Equal("2013-03-14", CaptureFixture.DeploymentFieldFor(result.Xml, DeployableRevision, "LastChangeTime"));
    }
}
