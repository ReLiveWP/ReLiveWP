using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;

namespace Microsoft.Extensions.Hosting;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceEndpoints(this IHostApplicationBuilder builder)
    {
        builder.Configuration.Sources.Insert(0, CreateIniSource());
        builder.Configuration.AddIniFile("services.ini", true, true);
        return builder;
    }

    public static IHostBuilder AddServiceEndpoints(this IHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.Sources.Insert(0, CreateIniSource()));

    private static IniStreamConfigurationSource CreateIniSource() =>
        new() { Stream = typeof(ServiceDefaultsExtensions).Assembly
            .GetManifestResourceStream("ReLiveWP.ServiceDefaults.services.ini")! };
}
