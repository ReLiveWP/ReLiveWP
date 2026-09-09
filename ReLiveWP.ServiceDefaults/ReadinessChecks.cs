using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class ReadinessCheckExtensions
{
    public const string ReadyTag = "ready";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    // the probe runs inside a fresh DI scope, so resolving a DbContext from it is fine.
    // /health runs these, /alive does not
    public static IServiceCollection AddReadinessCheck(this IServiceCollection services,
                                                       string name,
                                                       Func<IServiceProvider, CancellationToken, Task<bool>> probe,
                                                       TimeSpan? timeout = null)
    {
        services.AddHealthChecks().Add(new HealthCheckRegistration(
            name, sp => new ProbeHealthCheck(sp, probe), HealthStatus.Unhealthy, [ReadyTag], timeout ?? DefaultTimeout));

        return services;
    }

    private sealed class ProbeHealthCheck(
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task<bool>> probe) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await probe(services, cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy($"{context.Registration.Name} probe failed");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message, ex);
            }
        }
    }
}
