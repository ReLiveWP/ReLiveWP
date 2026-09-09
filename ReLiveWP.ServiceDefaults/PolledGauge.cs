using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReLiveWP.ServiceDefaults;

// an ObservableGauge callback runs on the collect thread, so anything that needs IO to sample
// (redis, postgres) samples on its own timer and the gauge just reads the last value
public sealed class PolledGauge : BackgroundService
{
    private readonly TimeSpan interval;
    private readonly Func<CancellationToken, Task<long>> sample;
    private readonly ILogger logger;
    private readonly ObservableGauge<long> gauge;
    private long value;

    public PolledGauge(string name, string? unit, TimeSpan interval, Func<CancellationToken, Task<long>> sample, ILogger<PolledGauge> logger)
    {
        this.interval = interval;
        this.sample = sample;
        this.logger = logger;
        gauge = ServiceTelemetry.Meter.CreateObservableGauge(name, () => Volatile.Read(ref value), unit);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await SampleOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SampleOnceAsync(CancellationToken ct)
    {
        try
        {
            Volatile.Write(ref value, await sample(ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sampling {Gauge} failed", gauge.Name);
        }
    }
}

public static class PolledGaugeExtensions
{
    // each sample runs in its own DI scope, so a DbContext is fine to resolve from the provider
    public static IServiceCollection AddPolledGauge(this IServiceCollection services,
                                                    string name,
                                                    TimeSpan interval,
                                                    Func<IServiceProvider, CancellationToken, Task<long>> sample,
                                                    string? unit = null) =>
        services.AddSingleton<IHostedService>(sp => new PolledGauge(name, unit, interval, async ct =>
        {
            await using var scope = sp.CreateAsyncScope();
            return await sample(scope.ServiceProvider, ct);
        }, sp.GetRequiredService<ILogger<PolledGauge>>()));
}
