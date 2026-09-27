using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class SyncUpdatesBenchmarks
{
    private static readonly int[] Rounds = [5, 12, 23];

    private DbContextOptions<UpdatesDbContext> options = null!;
    private Dictionary<int, string> requests = null!;

    [Params(5, 12, 23)]
    public int Round { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        options = BenchmarkCatalog.BuildOptions();
        requests = BenchmarkCatalog.ReadSyncRequests(Rounds);
    }

    [Benchmark]
    public async Task<int> SyncUpdates()
    {
        using var db = new UpdatesDbContext(options);
        var result = await new UpdateService(db).SyncUpdatesAsync(requests[Round]);
        return result.Xml.Length;
    }
}

[Config(typeof(BenchmarkConfig))]
public class GetExtendedUpdateInfoBenchmarks
{
    private DbContextOptions<UpdatesDbContext> options = null!;
    private string request = null!;

    [GlobalSetup]
    public void Setup()
    {
        options = BenchmarkCatalog.BuildOptions();
        request = BenchmarkCatalog.ReadExtendedRequest(1);
    }

    [Benchmark]
    public async Task<int> GetExtendedUpdateInfo()
    {
        using var db = new UpdatesDbContext(options);
        var result = await new UpdateService(db).GetExtendedUpdateInfoAsync(request);
        return result.Xml.Length;
    }
}
