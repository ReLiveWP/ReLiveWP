using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReLiveWP.Services.MediaProxy.Tests;

public sealed class MediaProxyHost : IDisposable
{
    private readonly WebApplicationFactory<Program> factory;

    public int PublicPort { get; }
    public int InternalPort { get; }

    public MediaProxyHost() : this(FindFreePorts())
    {
    }

    private MediaProxyHost((int Public, int Internal) ports) : this(ports.Public, ports.Internal)
    {
    }

    public static MediaProxyHost StartOnPorts(int publicPort, int internalPort) => new(publicPort, internalPort);

    private MediaProxyHost(int publicPort, int internalPort)
    {
        PublicPort = publicPort;
        InternalPort = internalPort;

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Kestrel:Endpoints:Public:Url", $"http://127.0.0.1:{publicPort}");
            builder.UseSetting("Kestrel:Endpoints:Internal:Url", $"http://127.0.0.1:{internalPort}");
            builder.UseSetting("Media:ProxyKeys:v2", SignedUrls.CurrentKey);
        });

        factory.UseKestrel();
        factory.StartServer();
    }

    public HttpClient CreatePublicClient() => new() { BaseAddress = new Uri($"http://127.0.0.1:{PublicPort}") };

    public HttpClient CreateInternalClient() => new() { BaseAddress = new Uri($"http://127.0.0.1:{InternalPort}") };

    public void Dispose() => factory.Dispose();

    private static (int Public, int Internal) FindFreePorts()
    {
        using var first = new TcpListener(IPAddress.Loopback, 0);
        using var second = new TcpListener(IPAddress.Loopback, 0);
        first.Start();
        second.Start();

        var publicPort = ((IPEndPoint)first.LocalEndpoint).Port;
        var internalPort = ((IPEndPoint)second.LocalEndpoint).Port;
        return (publicPort, internalPort);
    }
}
