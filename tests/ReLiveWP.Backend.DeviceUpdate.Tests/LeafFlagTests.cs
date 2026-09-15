using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// IsLeaf decides what the client sends back in InstalledNonLeafUpdateIDs, so it has to be defined
// over the catalog we serve rather than replayed from upstream. It narrows and never widens:
// upstream knows about dependents outside the slice we hold, and promoting one of those to a leaf
// would stop the client reporting an id our own prerequisites might later need.
public class LeafFlagTests : IAsyncLifetime
{
    private static readonly Guid Depended = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Orphan = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Dependent = new("33333333-3333-3333-3333-333333333333");

    private SqliteConnection connection = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        // Upstream called it a leaf, but our own catalog has something depending on it.
        db.Updates.Add(Revision(100, Depended, reported: true));

        // Upstream called it a non-leaf; nothing here names it. This is the 66-row case.
        db.Updates.Add(Revision(200, Orphan, reported: false));

        db.Updates.Add(Revision(300, Dependent, reported: true));
        db.Prerequisites.Add(new UpdatePrerequisite
        {
            UpdateRevisionId = 300,
            PrerequisiteUpdateId = Depended,
            GroupId = 0
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        connection.Dispose();
        return Task.CompletedTask;
    }

    private static Update Revision(long revisionId, Guid updateId, bool reported) =>
        new()
        {
            RevisionId = revisionId,
            UpdateId = updateId,
            RevisionNumber = 1,
            UpdateType = UpdateType.Detectoid,
            DeploymentAction = "Evaluate",
            IsLeafReported = reported,
            IsLeaf = reported,
            Metadata = new UpdateMetadata { SyncXml = "" }
        };

    private UpdatesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<UpdatesDbContext>().UseSqlite(connection).Options);

    private async Task<Dictionary<long, bool>> RecomputeAsync()
    {
        using var db = NewContext();
        await CatalogWriter.RecomputeLeafFlagsAsync(db);

        return await db.Updates.AsNoTracking().ToDictionaryAsync(u => u.RevisionId, u => u.IsLeaf);
    }

    [Fact]
    public async Task ARevisionOurOwnPrerequisitesNameIsNotALeaf()
    {
        var leaves = await RecomputeAsync();

        Assert.False(leaves[100]);
    }

    [Fact]
    public async Task UpstreamsNonLeafIsNeverPromotedToALeaf()
    {
        var leaves = await RecomputeAsync();

        Assert.False(leaves[200]);
    }

    [Fact]
    public async Task ARevisionNothingDependsOnStaysALeaf()
    {
        var leaves = await RecomputeAsync();

        Assert.True(leaves[300]);
    }

    [Fact]
    public async Task RecomputingTwiceChangesNothingTheSecondTime()
    {
        await RecomputeAsync();

        using var db = NewContext();
        Assert.Equal(0, await CatalogWriter.RecomputeLeafFlagsAsync(db));
    }
}
