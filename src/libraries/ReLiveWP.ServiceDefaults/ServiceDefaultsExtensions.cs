using System.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.Net.ClientFactory;
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
using ReLiveWP.ServiceDefaults;
using ClientInterceptorRegistration = Grpc.Net.ClientFactory.InterceptorRegistration;

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
        AddServiceDefaults(builder.Services, builder.Configuration);
        return builder;
    }

    // classic Startup hosts register their health checks by hand, everything else matches the overload above
    public static IHostBuilder AddServiceEndpoints(this IHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => ApplyEndpointSources(config))
            .ConfigureLogging(AddOpenTelemetryLogging)
            .ConfigureServices((context, services) =>
            {
                AddOpenTelemetry(services, context.Configuration);
                AddServiceDefaults(services, context.Configuration);
            });

    private static void AddServiceDefaults(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        services.Configure<SupportOptions>(configuration.GetSection(SupportOptions.SectionName));
        services.AddSingleton<SupportLinks>();
    }

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
        AddOpenTelemetryLogging(builder.Logging);
        AddOpenTelemetry(builder.Services, builder.Configuration);
        return builder;
    }

    private static void AddOpenTelemetryLogging(ILoggingBuilder logging)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(ServiceTelemetry.ServiceName, serviceVersion: ServiceTelemetry.ServiceVersion);

        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });
    }

    private static void AddOpenTelemetry(IServiceCollection services, IConfiguration configuration)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceTelemetry.ServiceName, serviceVersion: ServiceTelemetry.ServiceVersion))
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(ServiceTelemetry.ServiceName)
                    .AddMeter("Npgsql")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(ServiceTelemetry.ServiceName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.UseOtlpExporter();

        services.AddSingleton<GrpcTelemetryInterceptor>();
        services.Configure<GrpcServiceOptions>(o => o.Interceptors.Add<GrpcTelemetryInterceptor>());
        services.ConfigureAll<GrpcClientFactoryOptions>(o => o.InterceptorRegistrations.Add(
            new ClientInterceptorRegistration(InterceptorScope.Channel, sp => sp.GetRequiredService<GrpcTelemetryInterceptor>())));
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddDefaultHealthChecks();
        return builder;
    }

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
