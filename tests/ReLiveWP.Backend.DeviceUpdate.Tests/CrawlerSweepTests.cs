using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class CrawlerSweepTests : IDisposable
{
    private const int DetectoidCount = 20;
    private const int ReportCap = 8;

    private readonly SqliteConnection connection;

    public CrawlerSweepTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private UpdatesDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<UpdatesDbContext>()
            .UseSqlite(connection)
            .Options;
        return new UpdatesDbContext(options);
    }

    private static WsusUpstreamClient NewUpstream(HttpClient http) =>
        new(http, Options.Create(new CrawlerOptions
        {
            ClientEndpoint = "http://localhost/v6/ClientWebService/client.asmx",
        }));

    // The upstream server rejects any report bigger than its cap, and the crawler's whole discovery
    // strategy is to report far more detectoids than a real device would. It has to split its way
    // down to an answerable size, and it has to do that without dropping the leaves that only
    // become visible when their gating detectoid is reported.
    [Fact]
    public async Task SplitsOversizedReportsAndStillCollectsEveryGatedLeaf()
    {
        var handler = new FakeUpdateServerHandler(DetectoidCount, ReportCap);
        using var http = new HttpClient(handler);

        using var db = NewContext();
        var crawler = new Crawler(db, NewUpstream(http), NullLogger<Crawler>.Instance);

        await crawler.CrawlAsync();

        var leaves = await db.Updates.CountAsync(u => u.IsLeaf);
        var detectoids = await db.Updates.CountAsync(u => !u.IsLeaf);

        Assert.Equal(DetectoidCount, detectoids);
        Assert.Equal(DetectoidCount, leaves);
        Assert.True(handler.FaultCount > 0, "the fake server never rejected a report, so no split was exercised");
    }

    [Fact]
    public async Task KeepsWhatItFoundWhenTheServerStopsAnswering()
    {
        var handler = new FakeUpdateServerHandler(DetectoidCount, ReportCap);
        using var http = new HttpClient(handler);

        using var db = NewContext();
        var crawler = new Crawler(db, NewUpstream(http), NullLogger<Crawler>.Instance);

        await crawler.CrawlAsync();

        Assert.NotEmpty(await db.Updates.ToListAsync());
        Assert.NotEmpty(await db.Prerequisites.ToListAsync());
    }
}
