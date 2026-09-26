using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.ServiceDefaults.Media;

public static class MediaClientExtensions
{
    private const string InternalEndpointKey = "Endpoints:MediaProxy:Internal";
    private const string PublicEndpointKey = "Endpoints:MediaProxy:Public";

    private static readonly TimeSpan UploadTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ProxyFetchTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan ConnectionLifetime = TimeSpan.FromMinutes(15);

    public static IHostApplicationBuilder AddMediaPipelineClient(this IHostApplicationBuilder builder)
    {
        var http = CreateUnmanagedClient(builder.Configuration, InternalEndpointKey, UploadTimeout);

        builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<MediaPipelineClient>(sp, http));
        return builder;
    }

    public static IHostApplicationBuilder AddMediaProxyClient(this IHostApplicationBuilder builder)
    {
        var http = CreateUnmanagedClient(builder.Configuration, PublicEndpointKey, ProxyFetchTimeout);

        builder.Services.TryAddSingleton(MediaProxyUrlSigner.FromConfiguration(builder.Configuration));
        builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<MediaProxyClient>(sp, http));
        return builder;
    }

    private static HttpClient CreateUnmanagedClient(IConfiguration configuration, string endpointKey, TimeSpan timeout)
    {
        var endpoint = configuration[endpointKey]
            ?? throw new InvalidOperationException($"{endpointKey} is not configured.");

        return new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = ConnectionLifetime })
        {
            BaseAddress = new Uri(endpoint),
            Timeout = timeout,
        };
    }
}
