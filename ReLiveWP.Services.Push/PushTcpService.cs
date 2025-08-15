using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.Services.Push;

public class PushTcpService : IHostedService
{
    private readonly TcpListener tcpListener;
    public PushTcpService()
    {
        tcpListener = new TcpListener(IPAddress.Any, 2345);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        tcpListener.Start();
        Task.Run(TcpListenerTask);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task TcpListenerTask()
    {
        TcpClient tcpClient;
        while ((tcpClient = await tcpListener.AcceptTcpClientAsync()) != null)
        {

        }
    }
}
