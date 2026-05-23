using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReLiveWP.Services.Push.Model;
using ReLiveWP.Services.Push.WireFormat;

namespace ReLiveWP.Services.Push;

public class PushTcpService : IHostedService
{
    private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, 6969);
    private readonly ILogger<PushTcpService> logger;
    private readonly X509Certificate2 serverCert = new X509Certificate2("push.int.relivewp.net.pfx");

    public PushTcpService(ILogger<PushTcpService> logger)
    {
        this.logger = logger;
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
        TcpClient client;
        while ((client = await tcpListener.AcceptTcpClientAsync()) != null)
        {
            var stream = client.GetStream();
            var sslStream = new SslStream(stream, false, ValidateClientCert);

            try
            {
#pragma warning disable SYSLIB0039 // Type or member is obsolete
                await sslStream.AuthenticateAsServerAsync(
                    serverCert,
                    clientCertificateRequired: false, 
                    enabledSslProtocols: SslProtocols.Tls,
                    checkCertificateRevocation: false);
#pragma warning restore SYSLIB0039 // Type or member is obsolete

                logger.LogInformation("SSL handshake complete");

                var session = new PushSession(client, sslStream);
                _ = Task.Run(() => session.RunLoop());

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SSL error: {Message}", ex.Message);
            }
        }
    }

    static bool ValidateClientCert(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        Console.WriteLine("Validating client cert...");

        if (certificate == null)
        {
            Console.WriteLine("No client certificate provided.");
            return true;
        }

        // Example: trust any client cert 
        Console.WriteLine("Client certificate subject: " + certificate.Subject);
        return true;
    }
}
