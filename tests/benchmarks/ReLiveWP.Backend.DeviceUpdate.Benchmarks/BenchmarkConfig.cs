using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace ReLiveWP.Backend.DeviceUpdate.Benchmarks;

// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for net11.0, so its CsProj toolchain throws while
// validating the SDK. In-process sidesteps that; these operations are hundreds of milliseconds, so
// the lost process isolation costs nothing we can measure.
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithStrategy(RunStrategy.Monitoring)
            .WithWarmupCount(2)
            .WithIterationCount(10)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddExporter(MarkdownExporter.GitHub);
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
