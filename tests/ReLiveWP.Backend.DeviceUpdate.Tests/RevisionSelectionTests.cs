using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// Every UpdateId in the crawled catalog and in the capture corpus is unique, so the
// highest-revision-per-update pick never actually runs there. These build the multi-revision case
// by hand.
public class RevisionSelectionTests : IAsyncLifetime
{
    private static readonly Guid Detectoid = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Missing = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Fallback = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Tied = new("44444444-4444-4444-4444-444444444444");

    private const long DetectoidRevision = 100;

    private SqliteConnection connection = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        db.Updates.Add(Revision(DetectoidRevision, Detectoid, 1, UpdateType.Detectoid, "Evaluate", isLeaf: false));

        db.Updates.Add(Revision(201, Fallback, 1, UpdateType.Software, "Install", isLeaf: true));
        db.Updates.Add(Revision(202, Fallback, 2, UpdateType.Software, "Install", isLeaf: true));
        db.Updates.Add(Revision(203, Fallback, 3, UpdateType.Software, "Install", isLeaf: true));

        db.Updates.Add(Revision(301, Tied, 7, UpdateType.Software, "Install", isLeaf: true));
        db.Updates.Add(Revision(302, Tied, 7, UpdateType.Software, "Install", isLeaf: true));

        foreach (var revision in (long[])[201, 202, 203, 301, 302])
            db.Prerequisites.Add(new UpdatePrerequisite
            {
                UpdateRevisionId = revision,
                PrerequisiteUpdateId = Detectoid,
                GroupId = 0
            });

        db.Prerequisites.Add(new UpdatePrerequisite
        {
            UpdateRevisionId = 203,
            PrerequisiteUpdateId = Missing,
            GroupId = 0
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        connection.Dispose();
        return Task.CompletedTask;
    }

    private static Update Revision(long revisionId, Guid updateId, int revisionNumber, UpdateType type, string action, bool isLeaf) =>
        new()
        {
            RevisionId = revisionId,
            UpdateId = updateId,
            RevisionNumber = revisionNumber,
            UpdateType = type,
            DeploymentAction = action,
            IsLeaf = isLeaf,
            LastChangeTime = new DateTime(2011, 1, 1),
            Metadata = new UpdateMetadata
            {
                SyncXml = $"<UpdateIdentity UpdateID=\"{updateId}\" RevisionNumber=\"{revisionNumber}\" />"
            }
        };

    private UpdatesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<UpdatesDbContext>().UseSqlite(connection).Options);

    private static string RequestWith(params long[] installed) => RequestWith(installed, []);

    private static string RequestWith(long[] installed, long[] cached)
    {
        var ids = string.Concat(installed.Select(i => $"<int>{i}</int>"));
        var cachedIds = string.Concat(cached.Select(i => $"<int>{i}</int>"));

        return $"""
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <SyncUpdates xmlns="http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService">
                  <parameters>
                    <InstalledNonLeafUpdateIDs>{ids}</InstalledNonLeafUpdateIDs>
                    <OtherCachedUpdateIDs>{cachedIds}</OtherCachedUpdateIDs>
                  </parameters>
                </SyncUpdates>
              </s:Body>
            </s:Envelope>
            """;
    }

    private async Task<List<long>> OfferedAsync()
    {
        using var db = NewContext();
        var result = await new UpdateService(db).SyncUpdatesAsync(RequestWith(DetectoidRevision));

        return [.. CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates")];
    }

    [Fact]
    public async Task FallsBackToAnOlderRevisionWhenTheNewestIsUnsatisfiable()
    {
        var offered = await OfferedAsync();

        Assert.Contains(202, offered);
        Assert.DoesNotContain(203, offered);
        Assert.DoesNotContain(201, offered);
    }

    [Fact]
    public async Task OffersExactlyOneRevisionPerUpdate()
    {
        var offered = await OfferedAsync();

        Assert.Single(offered, id => id is 201 or 202 or 203);
        Assert.Single(offered, id => id is 301 or 302);
    }

    [Fact]
    public async Task BreaksRevisionNumberTiesOnTheLowerRevisionId()
    {
        var offered = await OfferedAsync();

        Assert.Contains(301, offered);
        Assert.DoesNotContain(302, offered);
    }

    private async Task<List<long>> EvictedAsync(params long[] cached)
    {
        using var db = NewContext();
        var result = await new UpdateService(db).SyncUpdatesAsync(RequestWith([DetectoidRevision], cached));

        return [.. CaptureFixture.OutOfScopeIds(result.Xml).Order()];
    }

    [Fact]
    public async Task EvictsARevisionSupersededUnderTheSameUpdateId()
    {
        Assert.Equal([201], await EvictedAsync(201));
    }

    [Fact]
    public async Task KeepsTheRevisionItIsStillServing()
    {
        Assert.Empty(await EvictedAsync(202));
    }

    // Unsatisfiable, so it is not needed, and the device is moved onto 202 in the same round.
    [Fact]
    public async Task EvictsARevisionWhosePrerequisitesNoLongerHold()
    {
        using var db = NewContext();
        var result = await new UpdateService(db).SyncUpdatesAsync(RequestWith([DetectoidRevision], [203]));

        Assert.Contains(203L, CaptureFixture.OutOfScopeIds(result.Xml));
        Assert.Contains(202L, CaptureFixture.UpdateInfoIds(result.Xml, "NewUpdates"));
    }

    [Fact]
    public async Task EvictsARevisionTheCatalogDoesNotHoldAtAll()
    {
        Assert.Equal([987654], await EvictedAsync(987654));
    }
}
