using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;

namespace Microsoft.Extensions.Hosting;

public static class ServiceDefaultsExtensions
{
    // docker secrets are mounted here; KeyPerFile maps each file to a config key
    // (default "__" delimiter, e.g. JWT__Secret -> JWT:Secret). Optional so local runs are unaffected.
    private const string SecretsPath = "/run/secrets";

    public static IHostApplicationBuilder AddServiceEndpoints(this IHostApplicationBuilder builder)
    {
        ApplyEndpointSources(builder.Configuration);
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
            .GetManifestResourceStream(
#if RELEASE
            "ReLiveWP.ServiceDefaults.services.Prod.ini" 
#else
            "ReLiveWP.ServiceDefaults.services.ini"
#endif
            )! };
}
