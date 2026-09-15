using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.DeviceUpdate;
using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class CatalogCompilerTests : IAsyncLifetime
{
    private static readonly Guid Marker = new("6f2a1c74-9d3e-4b58-8f10-2c7e5a6d0b41");
    private static readonly Guid WindowsPhone7 = new("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8");
    private static readonly Guid PkgReLiveWP = new("3752d1fb-94e8-47ed-8302-e3fe267fd6e6");

    private SqliteConnection connection = null!;
    private string manifestPath = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        manifestPath = Directory.CreateTempSubdirectory("relivewp-catalog").FullName;
        WriteManifest(revision: 1, data: "7.10.8162.217");
    }

    public Task DisposeAsync()
    {
        connection.Dispose();
        Directory.Delete(manifestPath, recursive: true);
        return Task.CompletedTask;
    }

    private void WriteManifest(int revision, string data) =>
        File.WriteAllText(Path.Combine(manifestPath, "relivewp.xml"),
            $"""
            <Catalog>
              <Update id="{Marker}" slug="relivewp-platform-present" type="Detectoid" action="Evaluate" isLeaf="false" revision="{revision}">
                <Prerequisites>
                  <AnyOf isCategory="true">
                    <Update id="{WindowsPhone7}" />
                  </AnyOf>
                </Prerequisites>
                <IsInstalled>
                  <PackageVersion package="{PkgReLiveWP}" comparison="GreaterThanOrEqualTo" value="{data}" />
                </IsInstalled>
                <Localization lang="en">
                  <Title>ReLiveWP platform present</Title>
                </Localization>
              </Update>
            </Catalog>
            """);

    private UpdatesDbContext NewContext() =>
        new(new DbContextOptionsBuilder<UpdatesDbContext>().UseSqlite(connection).Options);

    private async Task<int> CompileAsync()
    {
        using var db = NewContext();
        var options = Options.Create(new PackageOptions());
        return await new CatalogCompiler(db, options, NullLogger<CatalogCompiler>.Instance).CompileAsync(manifestPath);
    }

    [Fact]
    public async Task WritesAnAuthoredRevisionInTheReservedBand()
    {
        Assert.Equal(1, await CompileAsync());

        using var db = NewContext();
        var written = await db.Updates.AsNoTracking().SingleAsync();

        Assert.Equal(Marker, written.UpdateId);
        Assert.Equal(UpdateOrigin.Authored, written.Origin);
        Assert.Equal(UpdateType.Detectoid, written.UpdateType);
        Assert.InRange(written.RevisionId, RevisionIdAllocator.Floor, RevisionIdAllocator.Ceiling);
    }

    // The manifest is the source of truth, so running the compiler again on an unchanged one is a
    // no-op rather than a second revision.
    [Fact]
    public async Task RecompilingAnUnchangedManifestWritesNothing()
    {
        await CompileAsync();

        Assert.Equal(0, await CompileAsync());

        using var db = NewContext();
        Assert.Equal(1, await db.Updates.CountAsync());
    }

    [Fact]
    public async Task RefusesContentThatChangedWithoutARevisionBump()
    {
        await CompileAsync();
        WriteManifest(revision: 1, data: "7.10.8162.218");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(CompileAsync);

        Assert.Contains("Bump RevisionNumber", error.Message);
    }

    [Fact]
    public async Task ABumpedRevisionGetsItsOwnRevisionId()
    {
        await CompileAsync();
        WriteManifest(revision: 2, data: "7.10.8162.218");
        await CompileAsync();

        using var db = NewContext();
        var revisions = await db.Updates.AsNoTracking().OrderBy(u => u.RevisionNumber).ToListAsync();

        Assert.Equal(2, revisions.Count);
        Assert.NotEqual(revisions[0].RevisionId, revisions[1].RevisionId);
        Assert.All(revisions, r => Assert.Equal(Marker, r.UpdateId));
    }

    // Serving reads the stored blobs, so an authored revision is only useful if the crawler's own
    // parsers can read what the compiler wrote.
    [Fact]
    public async Task StoredMetadataReadsBackThroughTheCrawlerParsers()
    {
        await CompileAsync();

        using var db = NewContext();
        var written = await db.Updates
            .AsNoTracking()
            .Include(u => u.Metadata)
            .Include(u => u.Prerequisites)
            .Include(u => u.Fragments)
            .SingleAsync();

        var reparsed = SyncUpdatesParser.ParseMetadataOnly(written.Metadata!.SyncXml);
        Assert.Equal(Marker, reparsed.UpdateId);

        var prerequisite = Assert.Single(written.Prerequisites);
        Assert.Equal(WindowsPhone7, prerequisite.PrerequisiteUpdateId);
        Assert.True(prerequisite.IsCategory);

        Assert.Contains(written.Fragments, f => f.FragmentType == FragmentTypes.Extended);
        Assert.Contains(written.Fragments, f => f.FragmentType == FragmentTypes.LocalizedProperties && f.Language == "en");
    }

    // The client only reports non-leaf updates, so a marker that came out a leaf would be invisible
    // to the server no matter what the device evaluated. Nothing names it as a prerequisite yet, so
    // the declaration is the only thing holding it non-leaf.
    [Fact]
    public async Task TheMarkerDetectoidStaysNonLeafWithNothingDependingOnIt()
    {
        await CompileAsync();

        using var db = NewContext();
        Assert.False(await db.Updates.AsNoTracking().Select(u => u.IsLeaf).SingleAsync());
    }

    [Fact]
    public async Task AnUpdateThatDeclaresItselfALeafStaysOne()
    {
        File.WriteAllText(Path.Combine(manifestPath, "leaf.xml"),
            $"""
            <Catalog>
              <Update id="aaaaaaaa-0000-0000-0000-000000000009" slug="leafy" type="Software" action="Install" revision="1">
                <Prerequisites>
                  <Update id="{Marker}" />
                </Prerequisites>
              </Update>
            </Catalog>
            """);

        await CompileAsync();

        using var db = NewContext();
        var leaf = await db.Updates.AsNoTracking().SingleAsync(u => u.UpdateType == UpdateType.Software);

        Assert.True(leaf.IsLeaf);
    }
}
