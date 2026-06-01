using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Google.Protobuf;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Pdu;
using ReLiveWP.Services.Push.Pdu.Headers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReLiveWP.Services.Push.Session;

public enum PushSessionStateType
{
    Connecting,
    Reconnecting,
    Connected,
    Errored
}

public class PushSessionContext
{
    public string DeviceId { get; set; }
}

public class PushSession
{
    private const int MaxRedispatch = 4;
    private const ushort ErrorSessionInvalid = 0x4101;

    private readonly X509Certificate2 serverCert;

    private byte[] sessionToken;
    private PushSessionStateType state = PushSessionStateType.Connecting;

    private uint sendSequence = 0;
    private uint lastReceivedDataSeq = 0;

    private byte[] recvBuffer = new byte[8192];
    private int recvLen = 0;

    private readonly TcpClient client;
    private readonly SslStream sslStream;
    private readonly ILogger<PushSession> logger;
    private readonly Authentication.AuthenticationClient authenticationClient;
    private readonly SessionStore sessions;

    private readonly Channel<PDU> pduIncomingChannel = Channel.CreateUnbounded<PDU>();
    private readonly Channel<PDU> pduOutgoingChannel = Channel.CreateUnbounded<PDU>();
    private readonly Channel<PushDataPacket> pduInboundChannel = Channel.CreateUnbounded<PushDataPacket>();

    public PushSession(
        TcpClient client,
        SslStream sslStream,
        IConfiguration configuration,
        ILogger<PushSession> logger,
        Authentication.AuthenticationClient authenticationClient,
        SessionStore sessions)
    {
        this.client = client;
        this.sslStream = sslStream;
        this.logger = logger;
        this.authenticationClient = authenticationClient;
        this.sessions = sessions;

        this.serverCert = X509CertificateLoader.LoadPkcs12FromFile(configuration["Push:ServerCertPath"], configuration["Push:ServerCertPassword"]);
    }

    // completes the moment the device is authenticated and Connected, so the app layer
    // knows when it can register presence / drain queued notifications
    private readonly TaskCompletionSource connectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public ChannelReader<PushDataPacket> Inbound => pduInboundChannel.Reader;

    public Task Connected => connectedTcs.Task;

    public PushSessionContext Context { get; } = new PushSessionContext();

    public X509Certificate2 Certificate { get; private set; }

    public bool TrySendData(byte[] payload)
    {
        return pduOutgoingChannel.Writer.TryWrite(new PDU(PDUCommand.Data, NextSendSequence(),
            [new SequenceHeader { SequenceNumber = lastReceivedDataSeq }], payload));
    }

    public async Task RunLoop(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = cts.Token;

        Task receive = null, handle = null, send = null;
        try
        {
            await sslStream.AuthenticateAsServerAsync(
                serverCert,
                clientCertificateRequired: false,
#pragma warning disable SYSLIB0039 // Type or member is obsolete
                enabledSslProtocols: SslProtocols.Tls,
#pragma warning restore SYSLIB0039 // Type or member is obsolete
                checkCertificateRevocation: false);

            logger.LogInformation("SSL handshake complete");

            receive = PduIncomingLoop(token);
            handle = PduIncomingHandler(token);
            send = PduOutgoingLoop(token);

            await Task.WhenAny(receive, handle, send);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in Push run loop!");
        }
        finally
        {
            cts.Cancel();
            pduIncomingChannel.Writer.TryComplete();
            pduOutgoingChannel.Writer.TryComplete();
            try { await Task.WhenAll(receive, handle, send); } catch { /* shutting down, dont care */ }
            client.Close();
            connectedTcs.TrySetCanceled(); // unblock anyone awaiting a connect that never happened
        }
    }

    private async Task<bool> HandlePduConnecting(PDU pdu, ChannelWriter<PDU> writer, CancellationToken ct)
    {
        switch (pdu.Command)
        {
            case PDUCommand.Connect:
                {
                    var authHeader = pdu.Headers.OfType<AuthenticateHeader>().FirstOrDefault();
                    if (authHeader == null)
                    {
                        logger.LogWarning("Connect with no Authenticate header");
                        EndSession(); // malformed connect; tear the session down
                        return true;
                    }

                    Certificate = authHeader.Certificate;
                    if (!await ValidateCertificateAsync())
                    {
                        logger.LogError("Unable to validate certificate for push client!!");
                        EndSession();
                        return true;
                    }

                    var response = BuildConnectResponse(pdu); // mints sessionToken
                    await sessions.CreateAsync(Convert.ToHexString(sessionToken), Context.DeviceId, ct);
                    await writer.WriteAsync(response, ct);
                    state = PushSessionStateType.Connected;
                    connectedTcs.TrySetResult();
                    return true;
                }

            case PDUCommand.Reconnect:
                // throw the PDU over to the reconnecting state on the next dispatch pass.
                state = PushSessionStateType.Reconnecting;
                return false;

            default:
                logger.LogWarning("Unexpected {Command} while connecting", pdu.Command);
                return true;
        }
    }

    private async Task<bool> HandlePduReconnecting(PDU pdu, ChannelWriter<PDU> writer, CancellationToken ct)
    {
        if (pdu.Command != PDUCommand.Reconnect)
        {
            logger.LogWarning("Unexpected {Command} while reconnecting", pdu.Command);
            return true;
        }

        // the device echoes back the token we previously handed it in SessionInfo
        var presentedToken = pdu.Headers.OfType<SessionInfoHeader>().FirstOrDefault()?.Blob;
        var authHeader = pdu.Headers.OfType<AuthenticateHeader>().FirstOrDefault();

        if (authHeader == null)
        {
            logger.LogWarning("Reconnect with no Authenticate header");
            EndSession();
            return true;
        }

        Certificate = authHeader.Certificate;
        if (!await ValidateCertificateAsync())
        {
            logger.LogError("Unable to validate certificate for push client!!");
            EndSession();
            return true;
        }

        logger.LogInformation("Reconnect with token {Token}",
            presentedToken == null ? "<none>" : Convert.ToHexString(presentedToken));

        // a token we don't recognise (or one that isn't this device's) can't resume, reject
        // with 0x4101 so the device drops it and does a fresh Connect
        if (!await TryResumeSessionAsync(presentedToken))
        {
            logger.LogWarning("Unknown reconnect token; rejecting (0x4101) to force re-Connect");
            await writer.WriteAsync(BuildReconnectRejection(pdu), ct);
            state = PushSessionStateType.Connecting;
            return false;
        }

        sessionToken = presentedToken;

        await writer.WriteAsync(BuildReconnectResponse(pdu), ct);
        state = PushSessionStateType.Connected;
        connectedTcs.TrySetResult();
        return true;
    }

    private async Task<bool> HandlePduConnected(PDU pdu, ChannelWriter<PDU> writer, CancellationToken ct)
    {
        switch (pdu.Command)
        {
            case PDUCommand.KeepAlive:
                await writer.WriteAsync(BuildKeepAliveResponse(pdu), ct);
                break;

            case PDUCommand.Data:
                {
                    // the device dedups on the PDU sequence number; mirror that so a
                    // retransmit (which we still ack) isn't surfaced to the app twice
                    bool duplicate = pdu.SequenceNumber <= lastReceivedDataSeq;
                    if (!duplicate)
                        lastReceivedDataSeq = pdu.SequenceNumber;

                    await writer.WriteAsync(BuildAck(), ct);

                    if (!duplicate)
                    {
                        var binding = pdu.Headers.OfType<BindingHeader>().FirstOrDefault()?.Binding;
                        await pduInboundChannel.Writer.WriteAsync(new PushDataPacket(pdu.Data ?? [], binding), ct);
                    }
                    break;
                }

            case PDUCommand.Ack:
                // the device acknowledging our data :D
                break;

            case PDUCommand.Disconnect:
                await writer.WriteAsync(BuildDisconnectResponse(pdu), ct);
                EndSession(); // device is leaving; flush the response then close
                break;

            default:
                logger.LogWarning("Unhandled {Command} while connected", pdu.Command);
                break;
        }

        return true;
    }

    private Task<bool> HandlePduErrored(PDU pdu, ChannelWriter<PDU> writer, CancellationToken ct)
    {
        logger.LogWarning("Ignoring {Command} in errored state", pdu.Command);
        return Task.FromResult(true);
    }

    private async Task PduIncomingHandler(CancellationToken ct)
    {
        try
        {
            await foreach (var pdu in pduIncomingChannel.Reader.ReadAllAsync(ct))
            {
                logger.LogInformation("Got PDU command {Command} (seq {Seq})", pdu.Command, pdu.SequenceNumber);

                var writer = pduOutgoingChannel.Writer;

                bool consumed = false;
                for (int i = 0; i < MaxRedispatch && !consumed; i++)
                {
                    consumed = state switch
                    {
                        PushSessionStateType.Connecting => await HandlePduConnecting(pdu, writer, ct),
                        PushSessionStateType.Reconnecting => await HandlePduReconnecting(pdu, writer, ct),
                        PushSessionStateType.Connected => await HandlePduConnected(pdu, writer, ct),
                        PushSessionStateType.Errored => await HandlePduErrored(pdu, writer, ct),
                        _ => true,
                    };
                }

                if (!consumed)
                    logger.LogWarning("PDU {Command} not consumed after {Max} re-dispatches", pdu.Command, MaxRedispatch);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session handler error");
            pduOutgoingChannel.Writer.TryComplete(ex);
        }
        finally
        {
            pduInboundChannel.Writer.TryComplete();
            pduOutgoingChannel.Writer.TryComplete();
        }
    }

    private async Task PduIncomingLoop(CancellationToken ct)
    {
        try
        {
            while (true)
            {
                // read all the PDUs!
                while (recvLen >= 9)
                {
                    int total = 9 + (recvBuffer[2] | (recvBuffer[3] << 8));

                    if (total > recvBuffer.Length)
                    {
                        // TODO: do we really need this if the client is capped at 4096?
                        Array.Resize(ref recvBuffer, total);
                        break;
                    }

                    if (recvLen < total)
                        break;

                    var bytes = recvBuffer.AsSpan(0, total).ToArray();

                    // shift any trailing bytes (the next PDU) down to the start.
                    Buffer.BlockCopy(recvBuffer, total, recvBuffer, 0, recvLen - total);
                    recvLen -= total;

                    logger.LogDebug("Incoming PDU data: {Outgoing}", Convert.ToHexString(bytes, 0, total));

                    if (!PDU.TryParse(bytes, 0, total, out var pdu))
                    {
                        logger.LogWarning("Failed to parse {Total}-byte PDU; closing session", total);
                        return;
                    }

                    await pduIncomingChannel.Writer.WriteAsync(pdu, ct);
                }

                if (recvLen == recvBuffer.Length)
                    Array.Resize(ref recvBuffer, recvBuffer.Length * 2);

                int n = await sslStream.ReadAsync(recvBuffer.AsMemory(recvLen), ct);
                if (n == 0)
                    return; // clean EOF

                recvLen += n;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Receive loop error");
            pduIncomingChannel.Writer.TryComplete(ex);
        }
        finally
        {
            pduIncomingChannel.Writer.TryComplete();
        }
    }

    private async Task PduOutgoingLoop(CancellationToken ct)
    {
        try
        {
            await foreach (var pdu in pduOutgoingChannel.Reader.ReadAllAsync(ct))
            {
                logger.LogInformation("Sending PDU {Command} (seq {Seq})", pdu.Command, pdu.SequenceNumber);
                var data = pdu.Serialize();
                logger.LogDebug("Outgoing PDU data: {Outgoing}", Convert.ToHexString(data));
                await sslStream.WriteAsync(data, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Send loop error");
        }
    }


    private async Task<bool> TryResumeSessionAsync(byte[] token)
    {
        if (token == null)
            return false;

        // the cert already authenticated the device; the token only has to match a session
        // we issued to that same device. 
        var record = await sessions.FindAsync(Convert.ToHexString(token));
        if (record == null || record.DeviceId != Context.DeviceId)
            return false;

        await sessions.TouchAsync(record.Token);
        return true;
    }

    private PDU BuildConnectResponse(PDU connect)
    {
        sessionToken = Guid.NewGuid().ToByteArray();

        var headers = new PDUHeader[]
        {
            new SequenceHeader { SequenceNumber = connect.SequenceNumber },
            new SessionInfoHeader { Blob = sessionToken },

            new SessionConfigHeader
            {
                MaxPayloadSize = 4096,
                WindowSize = 16,          
                // 0/0 -> keeps the device's built-in keepalive window
                MinKeepAliveInterval = 0,
                MaxKeepAliveInterval = 0,
            },

            // 0x18 - mandatory presence.
            new TransportSessionConfigHeader { TransportConfig = 0 },
        };

        return new PDU(PDUCommand.ConnectResponse, NextSendSequence(), headers, []);
    }

    private PDU BuildReconnectResponse(PDU reconnect)
    {
        var headers = new PDUHeader[]
        {
            new SequenceHeader { SequenceNumber = reconnect.SequenceNumber },
            new TransportSessionConfigHeader { TransportConfig = 0 },
        };

        return new PDU(PDUCommand.ReconnectResponse, NextSendSequence(), headers, []);
    }

    private PDU BuildReconnectRejection(PDU reconnect)
    {
        return new(PDUCommand.Error, NextSendSequence(),
            [new ErrorHeader { ErrorCode = ErrorSessionInvalid, ReferencedSequence = reconnect.SequenceNumber }], []);
    }

    private PDU BuildDisconnectResponse(PDU disconnect)
    {
        return new(PDUCommand.DisconnectResponse, NextSendSequence(),
                [new SequenceHeader { SequenceNumber = disconnect.SequenceNumber }], []);
    }

    private PDU BuildKeepAliveResponse(PDU keepAlive)
    {
        return new(PDUCommand.KeepAliveResponse, NextSendSequence(),
            [new SequenceHeader { SequenceNumber = keepAlive.SequenceNumber }], []);
    }

    private PDU BuildAck()
    {
        return new(PDUCommand.Ack, NextSendSequence(),
                [new SequenceHeader { SequenceNumber = lastReceivedDataSeq }], []);
    }

    private async Task<bool> ValidateCertificateAsync()
    {
        var resp = await authenticationClient.ValidateDeviceCertificateAsync(new ValidateDeviceCertificateRequest()
        {
            Certificate = ByteString.CopyFrom(Certificate.Export(X509ContentType.Cert))
        });

        if (!resp.Succeeded)
            return false;

        Context.DeviceId = resp.DeviceId;

        return true;
    }

    private void EndSession()
    {
        pduOutgoingChannel.Writer.TryComplete();
    }

    private uint NextSendSequence()
    {
        return Interlocked.Increment(ref sendSequence);
    }
}
