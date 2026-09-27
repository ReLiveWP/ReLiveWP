using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReLiveWP.Backend.DeviceUpdate.Data;

namespace ReLiveWP.Backend.DeviceUpdate.Benchmarks;

public static class BenchmarkCatalog
{
    public const string ProviderVariable = "DEVICEUPDATE_BENCH_PROVIDER";
    public const string ConnectionVariable = "DEVICEUPDATE_BENCH_CONNECTION";
    public const string CapturesVariable = "DEVICEUPDATE_BENCH_CAPTURES";

    public static DbContextOptions<UpdatesDbContext> BuildOptions()
    {
        var provider = Provider();
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable);
        var builder = new DbContextOptionsBuilder<UpdatesDbContext>();

        if (provider == "postgres")
        {
            builder.UseNpgsql(connection ?? DefaultPostgresConnection);
            return builder.Options;
        }

        var path = connection ?? DefaultSqliteFile();
        if (!File.Exists(path))
            throw new FileNotFoundException($"no catalog at {path}, set {ConnectionVariable}", path);

        builder.UseSqlite($"Data Source={path}");
        return builder.Options;
    }

    public static string Provider() =>
        (Environment.GetEnvironmentVariable(ProviderVariable) ?? "sqlite").Trim().ToLowerInvariant();

    public static string Describe()
    {
        var provider = Provider();
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)
            ?? (provider == "postgres" ? DefaultPostgresConnection : DefaultSqliteFile());

        return provider == "postgres" ? $"postgres: {connection}" : $"sqlite: {connection}";
    }

    public static Dictionary<int, string> ReadSyncRequests(IEnumerable<int> rounds)
    {
        var directory = CaptureDirectory("SyncUpdates");
        var requests = new Dictionary<int, string>();

        foreach (var round in rounds)
        {
            var path = Path.Combine(directory, $"{round}_SyncUpdatesRequest.xml");
            if (!File.Exists(path))
                throw new FileNotFoundException($"no captured request for round {round}", path);

            requests[round] = File.ReadAllText(path);
        }

        return requests;
    }

    public static string ReadExtendedRequest(int round)
    {
        var path = Path.Combine(CaptureDirectory("GetExtendedUpdateInfo"), $"{round}_GetExtendedUpdateInfoRequest.xml");
        if (!File.Exists(path))
            throw new FileNotFoundException($"no captured extended request for round {round}", path);

        return File.ReadAllText(path);
    }

    private const string DefaultPostgresConnection =
        "Host=localhost;Port=15432;Database=relive_deviceupdate;Username=relive;Password=relive";

    private static string CaptureDirectory(string name)
    {
        var configured = Environment.GetEnvironmentVariable(CapturesVariable);
        var directory = configured is null
            ? Path.Combine(ProjectDirectory(), name)
            : Path.Combine(configured, name);

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"no captures at {directory}, set {CapturesVariable}");

        return directory;
    }

    private static string DefaultSqliteFile() => Path.Combine(ProjectDirectory(), "updates.db");

    private static string ProjectDirectory() =>
        Path.Combine(RepositoryRoot(), "src", "backend", "ReLiveWP.Backend.DeviceUpdate");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ReLiveWP.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException($"no repository root above {AppContext.BaseDirectory}");
    }
}
