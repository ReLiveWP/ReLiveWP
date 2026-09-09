using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace ReLiveWP.ServiceDefaults;

public static class ServiceTelemetry
{
    public static string ServiceName { get; } = ResolveServiceName();
    public static string ServiceVersion { get; } = ResolveServiceVersion();

    public static ActivitySource ActivitySource { get; } = new(ServiceName, ServiceVersion);
    public static Meter Meter { get; } = new(ServiceName, ServiceVersion);

    public static InstrumentAdvice<double> RequestSeconds { get; } = new()
    {
        HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10],
    };

    public static InstrumentAdvice<double> LongLivedRequestSeconds { get; } = new()
    {
        HistogramBucketBoundaries = [1, 5, 15, 30, 60, 120, 300, 600, 900, 1800, 3600],
    };

    private static string ResolveServiceName() =>
        Assembly.GetEntryAssembly()?.GetName().Name?.ToLowerInvariant() ?? "relivewp";

    private static string ResolveServiceVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
            return informational;

        return assembly?.GetName().Version?.ToString() ?? "0.0.0";
    }
}
