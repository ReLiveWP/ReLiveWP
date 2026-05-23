using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ReLiveWP.Services.Push.WireFormat;

namespace ReLiveWP.Services.Push.Model;

public enum PushSessionState
{
    Connecting,
    Connected
}

public class PushSession
{
    private TcpClient client;
    private SslStream sslStream;
    private PushSessionState state = PushSessionState.Connecting;
    private uint sequence = 0;

    private X509Certificate certificate;

    public PushSession(TcpClient client, SslStream sslStream)
    {
        this.client = client;
        this.sslStream = sslStream;
    }

    public async Task RunLoop()
    {
        do
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await sslStream.ReadAsync(buffer);

            if (bytesRead == 0 || !PDU.TryParse(buffer, 0, bytesRead, out var pdu))
            {
                client.Close();
                break;
            }

            Interlocked.Increment(ref sequence);


            if (state == PushSessionState.Connecting)
            {
                await Task.Delay(10000);

                var authHeader = pdu.Headers.OfType<AuthenticateHeader>()
                    .FirstOrDefault();

                if (authHeader == null)
                {
                    client.Close();
                    break;
                }
                
                // TODO: verify this
                this.certificate = authHeader.Certificate;

                var sequenceHeader = new SequenceHeader() { SequenceNumber = sequence };
                var keepAlive = new OptimalKeepAliveHeader() { KeepAlive = 30 };
                var sessionConfig = new TransportSessionConfigHeader() { TransportConfig = 1770 };

                var resp = new PDU(1, 1, [sequenceHeader, keepAlive, sessionConfig], []);
                await sslStream.WriteAsync(resp.Serialize());

                state = PushSessionState.Connected;
            }

            
        } while (client.Connected);
    }
}
