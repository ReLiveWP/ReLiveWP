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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        tcpListener.Start();
        Task.Run(TcpListenerTask);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        tcpListener.Stop();
        return Task.CompletedTask;
    }

    private async Task TcpListenerTask()
    {
        TcpClient client;
        while ((client = await tcpListener.AcceptTcpClientAsync()) != null)
        {
            var stream = client.GetStream();
            var sslStream = new SslStream(stream, false);

            try
            {
                var scope = services.CreateScope();
                var transport = ActivatorUtilities.CreateInstance<PushSession>(scope.ServiceProvider, client, sslStream);
                var session = ActivatorUtilities.CreateInstance<NspSession>(scope.ServiceProvider, transport);
                _ = session.RunAsync();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SSL error: {Message}", ex.Message);
            }
        }
    }
}
