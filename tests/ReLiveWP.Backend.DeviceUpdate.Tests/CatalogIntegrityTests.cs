using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

[Collection(CaptureCollection.Name)]
public class CatalogIntegrityTests(CaptureFixture captures)
{
    // The phone was already warm when the capture started, so revisions it held before round 1 are
    // only ever named in ChangedUpdates with no metadata and the importer never learns them. The
    // checker exists to make that kind of hole loud instead of silent.
    [Fact]
    public async Task ReportsPrerequisitesTheCaptureNeverCarried()
    {
        using var db = captures.NewContext();
        var report = await CatalogVerifier.CheckCatalogAsync(db);

        Assert.False(report.IsClean);
        Assert.NotEmpty(report.UnresolvedPrerequisites);
        Assert.NotEmpty(report.UnsatisfiableRevisions);
    }

    [Fact]
    public async Task EveryUnresolvedReferencePointsOutsideTheCatalog()
    {
        using var db = captures.NewContext();
        var report = await CatalogVerifier.CheckCatalogAsync(db);
        var known = await captures.KnownUpdateIdsAsync();

        Assert.All(report.UnresolvedPrerequisites, r => Assert.DoesNotContain(r.MissingUpdateId, known));
        Assert.All(report.UnresolvedBundledUpdates, r => Assert.DoesNotContain(r.MissingUpdateId, known));
    }

    [Fact]
    public async Task UnsatisfiableRevisionsAreAllHeldInTheCatalog()
    {
        using var db = captures.NewContext();
        var report = await CatalogVerifier.CheckCatalogAsync(db);
        var known = await captures.KnownRevisionIdsAsync();

        Assert.All(report.UnsatisfiableRevisions, r => Assert.Contains(r, known));
    }
}
