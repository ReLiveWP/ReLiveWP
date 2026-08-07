using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace Microsoft.Extensions.Hosting;

public static class ServiceDefaultsExtensions
{
    // docker secrets are mounted here; KeyPerFile maps each file to a config key
    // (default "__" delimiter, e.g. JWT__Secret -> JWT:Secret). Optional so local runs are unaffected.
    private const string SecretsPath = "/run/secrets";

    public static IHostApplicationBuilder AddServiceEndpoints(this IHostApplicationBuilder builder)
    {
        ApplyEndpointSources(builder.Configuration);

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        builder.Services.Configure<ReLiveWP.ServiceDefaults.SupportOptions>(
            builder.Configuration.GetSection(ReLiveWP.ServiceDefaults.SupportOptions.SectionName));
        builder.Services.AddSingleton<ReLiveWP.ServiceDefaults.SupportLinks>();

        return builder;
    }

    public static IHostBuilder AddServiceEndpoints(this IHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => ApplyEndpointSources(config));

    private static void ApplyEndpointSources(IConfigurationBuilder config)
    {
        // embedded defaults first (lowest priority), then on-disk overrides, then secrets (highest)
        config.Sources.Insert(0, CreateIniSource());
        config.AddIniFile("services.ini", optional: true, reloadOnChange: true);

        // per-service container overrides (Kestrel binds, connection strings, cert paths) live in
        // an ini named after the service, e.g. ReLiveWP.Services.Activation.ini, mounted alongside it.
        var serviceName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (!string.IsNullOrEmpty(serviceName))
            config.AddIniFile($"{serviceName}.ini", optional: true, reloadOnChange: true);

        // gate on existence: PhysicalFileProvider throws on a missing dir, and local runs have no /run/secrets
        if (Directory.Exists(SecretsPath))
            config.AddKeyPerFile(SecretsPath, optional: true);
    }

    private static IniStreamConfigurationSource CreateIniSource() =>
        new() { Stream = typeof(ServiceDefaultsExtensions).Assembly
            .GetManifestResourceStream("ReLiveWP.ServiceDefaults.services.ini")! };

    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var serviceName = Assembly.GetEntryAssembly()!.GetName().Name!.ToLowerInvariant();

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0");

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(serviceName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddDefaultHealthChecks();
        return builder;
    }

    // overload for classic Startup-based hosts, which only get the config-source
    // wiring from the IHostBuilder AddServiceEndpoints and register checks by hand.
    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return services;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapDefaultHealthCheckEndpoints();
        return app;
    }

    // shared so Startup.Configure can map the same endpoints from inside UseEndpoints
    public static IEndpointRouteBuilder MapDefaultHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // /health runs every check (readiness); /alive only the "live"-tagged self check
        endpoints.MapHealthChecks(HealthEndpointPath);
        endpoints.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return endpoints;
    }
}
