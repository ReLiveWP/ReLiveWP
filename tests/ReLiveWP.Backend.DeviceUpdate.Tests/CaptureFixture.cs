using System.Formats.Tar;
using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// The 24 captured request/response pairs are one complete phone sync session against
// fe2.update.microsoft.com, so upstream's answers are the expected output for our own.
public class CaptureFixture : IAsyncLifetime
{
    public const int FirstRound = 5;
    public const int LastRound = 23;

    private SqliteConnection connection = null!;

    public string SyncDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Captures", "SyncUpdates");
    public string ExtendedDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Captures", "GetExtendedUpdateInfo");

    public async Task InitializeAsync()
    {
        await ExtractCapturesAsync();

        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        await db.Database.EnsureCreatedAsync();

        var importer = new CaptureImporter(db, NullLogger<CaptureImporter>.Instance);
        await importer.ImportAsync(SyncDirectory, ExtendedDirectory);
    }

    // The corpus ships compressed because it is 7.2MB of XML that nothing but these tests reads.
    private static async Task ExtractCapturesAsync()
    {
        var target = Path.Combine(AppContext.BaseDirectory, "Captures");
        var archive = Path.Combine(target, "fe2-session.tar.gz");

        if (!File.Exists(archive))
            throw new FileNotFoundException($"captured fe2 session missing at {archive}", archive);

        using var file = File.OpenRead(archive);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzip, target, overwriteFiles: true);
    }

    public Task DisposeAsync()
    {
        connection.Dispose();
        return Task.CompletedTask;
    }

    public UpdatesDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<UpdatesDbContext>()
            .UseSqlite(connection)
            .Options;
        return new UpdatesDbContext(options);
    }

    public UpdateService NewService(UpdatesDbContext db) => new(db);

    public string ReadRequest(int round) =>
        File.ReadAllText(Path.Combine(SyncDirectory, $"{round}_SyncUpdatesRequest.xml"));

    public string ReadResponse(int round) =>
        File.ReadAllText(Path.Combine(SyncDirectory, $"{round}_SyncUpdatesResponse.xml"));

    public string ReadExtendedRequest(int round) =>
        File.ReadAllText(Path.Combine(ExtendedDirectory, $"{round}_GetExtendedUpdateInfoRequest.xml"));

    public string ReadExtendedResponse(int round) =>
        File.ReadAllText(Path.Combine(ExtendedDirectory, $"{round}_GetExtendedUpdateInfoResponse.xml"));

    public async Task<HashSet<Guid>> KnownUpdateIdsAsync()
    {
        using var db = NewContext();
        return await db.Updates.Select(u => u.UpdateId).Distinct().ToHashSetAsync();
    }

    public async Task<HashSet<long>> KnownRevisionIdsAsync()
    {
        using var db = NewContext();
        return await db.Updates.Select(u => u.RevisionId).ToHashSetAsync();
    }

    public static HashSet<long> UpdateInfoIds(string xml, string section)
    {
        var container = XDocument.Parse(xml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == section);

        if (container is null)
            return [];

        return container
            .Elements()
            .Where(e => e.Name.LocalName == "UpdateInfo")
            .Select(e => (long)e.Elements().First(c => c.Name.LocalName == "ID"))
            .ToHashSet();
    }

    public static HashSet<long> ReportedIds(string requestXml, string section)
    {
        var container = XDocument.Parse(requestXml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == section);

        if (container is null)
            return [];

        return container.Elements()
            .Where(e => e.Name.LocalName == "int")
            .Select(e => (long)e)
            .ToHashSet();
    }

    public static bool IsFault(string xml) => xml.Contains("<ErrorCode>", StringComparison.Ordinal);

    public static HashSet<long> OutOfScopeIds(string xml)
    {
        var container = XDocument.Parse(xml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "OutOfScopeRevisionIDs");

        if (container is null)
            return [];

        return [.. container.Elements().Where(e => e.Name.LocalName == "int").Select(e => (long)e)];
    }

    public static string? DeploymentActionFor(string xml, long revisionId) =>
        DeploymentFieldFor(xml, revisionId, "Action");

    public static string? DeploymentFieldFor(string xml, long revisionId, string field) =>
        XDocument.Parse(xml)
            .Descendants()
            .Where(e => e.Name.LocalName == "UpdateInfo")
            .Where(e => (long)e.Elements().First(c => c.Name.LocalName == "ID") == revisionId)
            .Select(e => e.Elements().First(c => c.Name.LocalName == "Deployment"))
            .Select(d => (string?)d.Elements().First(c => c.Name.LocalName == field))
            .FirstOrDefault();
}

[CollectionDefinition(Name)]
public class CaptureCollection : ICollectionFixture<CaptureFixture>
{
    public const string Name = "captured sync session";
}
