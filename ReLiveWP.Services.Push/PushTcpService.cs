using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReLiveWP.Services.Push;

public class PushTcpService(ILogger<PushTcpService> logger) : IHostedService
{
    private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, 2345);
    private readonly X509Certificate2 serverCert = new X509Certificate2("172.16.0.2.pfx");


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

            using (var stream = client.GetStream())
            //using (var sslStream = new SslStream(networkStream, false,
            //    new RemoteCertificateValidationCallback(ValidateClientCert)))
            {
                try
                {
                    //await sslStream.AuthenticateAsServerAsync(
                    //    serverCert,
                    //    clientCertificateRequired: false, // require client cert
                    //    enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls,
                    //    checkCertificateRevocation: false);

                    //Console.WriteLine("SSL handshake complete");
                    //Console.WriteLine("Client cert: " + sslStream.RemoteCertificate?.Subject);

                    // Echo example
                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    //await sslStream.WriteAsync(buffer, 0, bytesRead);

                    string data = Convert.ToHexString(buffer);
                    logger.LogInformation("Got data {string}", data); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SSL error: " + ex.Message);
                }
            }

            client.Close();
        }
    }
    static bool ValidateClientCert(object sender, X509Certificate? certificate,
                                   X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        Console.WriteLine("Validating client cert...");

        if (certificate == null)
        {
            Console.WriteLine("No client certificate provided.");
            return true;
        }

        // Example: trust any client cert (NOT secure, just for demo)
        Console.WriteLine("Client certificate subject: " + certificate.Subject);
        return true;
    }
}
