using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using ReLiveWP.Services.Push.Nsp;
using ReLiveWP.Services.Push.Session;

namespace ReLiveWP.Services.Push.Services;

public class PushTcpService(
    ILogger<PushTcpService> logger,
    IServiceProvider services,
    IConfiguration configuration) : IHostedService
{
    private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, int.Parse(configuration["Push:Port"]));
    private readonly CancellationTokenSource stopping = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        tcpListener.Start();
        _ = Task.Run(() => AcceptLoopAsync(stopping.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        stopping.Cancel();
        tcpListener.Stop();
        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await tcpListener.AcceptTcpClientAsync(ct);
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Accept failed");
                continue;
            }

            _ = RunSessionAsync(client, ct);
        }
    }

    private async Task RunSessionAsync(TcpClient client, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        using (client)
        {
            try
            {
                var sslStream = new SslStream(client.GetStream(), false);
                var transport = ActivatorUtilities.CreateInstance<PushSession>(scope.ServiceProvider, client, sslStream);
                var session = ActivatorUtilities.CreateInstance<NspSession>(scope.ServiceProvider, transport);
                await session.RunAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Push session failed");
            }
        }
    }
}
