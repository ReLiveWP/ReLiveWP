using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Services;

public class MsnpGatewayService(
    IMsnpGatewaySessionStore sessions,
    IMsnpDeviceStore devices,
    Authentication.AuthenticationClient authenticationClient,
    Chat.ChatClient chatClient,
    IMsnpSessionWaker waker,
    IOptions<MessengerOptions> options,
    ILogger<MsnpGatewayService> logger)
{
    private const string SsoPolicy = "MBI_KEY_OLD";
    private const string ChallengeDigits = "0123456789";
    private const int ChallengeLength = 20;
    private static readonly string[] ServiceTargets = ["messengerclear.live.com", "messengerclear.live-int.com"];

    private enum SsoTicketVerification
    {
        Verified,
        Rejected,
        Unavailable,
    }

    private enum ChatSyncOutcome
    {
        Unchanged,
        SessionChanged,
        Evicted,
    }

    public async Task<(string SessionId, MsnpMessage Reply)> OpenAsync(
        MsnpMessage request, string? notificationUri, int? sessionTimeoutSeconds, CancellationToken ct)
    {
        var ver = Find(request, "VER");
        var cvr = Find(request, "CVR");
        var usr = Find(request, "USR");

        var replies = new List<MsnpCommand>();

        if (ver is not null)
            replies.Add(MsnpCommand.Create("VER", ver.TrId, "MSNP21"));

        if (cvr is not null)
        {
            // CVR args: langid, OS, OS-version, arch, client-name, client-version, brand, email, ...
            var clientVersion = cvr.Arguments.Length > 5 ? cvr.Arguments[5] : "1.0.0";
            replies.Add(MsnpCommand.Create("CVR", cvr.TrId,
                clientVersion, clientVersion, clientVersion,
                "https://relivewp.net/messenger", "https://relivewp.net/messenger"));
        }

        var sessionId = Guid.NewGuid().ToString("N");
        if (usr is null)
        {
            logger.LogInformation("MSNP gateway open without USR ({Verbs}), not creating a session",
                string.Join(' ', request.Commands.Select(c => c.Verb)));
            return (sessionId, new MsnpMessage(replies));
        }

        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var email = usr.Arguments.Length > 0 ? usr.Arguments[^1] : "";

        replies.Add(MsnpCommand.Create("USR", usr.TrId, "SSO", "S", SsoPolicy, nonce));

        await sessions.SaveAsync(new MsnpGatewaySession
        {
            SessionId = sessionId,
            Email = email,
            Nonce = nonce,
            Policy = SsoPolicy,
            NotificationUri = notificationUri ?? "",
            SessionTimeoutSeconds = sessionTimeoutSeconds,
            State = MsnpGatewaySessionState.AwaitingSsoTicket,
            CreatedAtUtc = DateTime.UtcNow,
        }, ct);

        logger.LogInformation("MSNP gateway open: session {SessionId}", sessionId);

        return (sessionId, new MsnpMessage(replies));
    }

    public async Task<MsnpMessage?> PollAsync(string sessionId, MsnpMessage request, TimeSpan lifespan, CancellationToken ct)
    {
        var session = await sessions.FindAsync(sessionId, ct);
        if (session is null)
            return null;

        Activity.Current?.SetTag("msnp.session_id", sessionId);
        Activity.Current?.SetTag("enduser.id", session.UserId);
        Activity.Current?.SetTag("msnp.verbs", string.Join(' ', request.Commands.Select(c => c.Verb)));

        if (Find(request, "OUT") is not null)
        {
            await SignOutAsync(session, ct);
            logger.LogInformation("MSNP gateway session {SessionId} closed by client OUT", sessionId);
            return null;
        }

        var replies = new List<MsnpCommand>();
        var sends = new List<MsnpCommand>();
        var sessionDirty = false;
        var statusChanged = false;

        foreach (var command in request.Commands)
        {
            var verb = command.Verb.ToUpperInvariant();
            if (session.State != MsnpGatewaySessionState.Authenticated && verb != "USR")
            {
                logger.LogWarning("MSNP gateway session {SessionId}: ignoring {Verb} before sign-in", sessionId, verb);
                continue;
            }

            switch (verb)
            {
                case "USR" when session.State == MsnpGatewaySessionState.AwaitingSsoTicket
                    && command.Arguments.Length > 1
                    && command.Arguments[0].Equals("SSO", StringComparison.OrdinalIgnoreCase):
                    {
                        var verification = await VerifySsoTicketAsync(session, command, ct);
                        if (verification != SsoTicketVerification.Verified)
                            return await RejectSignInAsync(session, command, verification, ct);

                        session.State = MsnpGatewaySessionState.Authenticated;
                        sessionDirty = true;
                        logger.LogInformation("MSNP gateway session {SessionId} authenticated for user {UserId}", sessionId, session.UserId);
                        replies.Add(MsnpCommand.Create("USR", command.TrId, "OK", session.Email, "1", "0"));
                        break;
                    }
                case "FSL":
                    {
                        replies.Add(MsnpCommand.Create("FSL", command.TrId, "OK", "0"));
                        break;
                    }
                case "CHL":
                    {
                        var challenge = RandomNumberGenerator.GetString(ChallengeDigits, ChallengeLength);
                        replies.Add(MsnpCommand.Create("CHL", "0", challenge));
                        break;
                    }
                case "QRY":
                    {
                        replies.Add(MsnpCommand.Create("QRY", command.TrId));
                        break;
                    }
                case "PUT":
                    {
                        statusChanged |= HandlePut(session, command);
                        sessionDirty = true;
                        replies.Add(MsnpCommand.Create("PUT", command.TrId, "OK", "0"));
                        break;
                    }
                case "SDG":
                    {
                        sends.Add(command);
                        break;
                    }
            }
        }

        var chatSync = await SyncChatAsync(session, statusChanged, ct);
        if (chatSync == ChatSyncOutcome.Evicted)
        {
            await SignOutAsync(session, ct);
            logger.LogInformation("MSNP gateway session {SessionId}: Chat evicted this endpoint, closing so the phone signs in fresh", sessionId);
            return null;
        }

        sessionDirty |= chatSync == ChatSyncOutcome.SessionChanged;

        foreach (var send in sends)
        {
            if (await RelayMessageAsync(session, send, ct) is { } failure)
                replies.Add(failure);
        }

        if (sessionDirty)
            await sessions.SaveAsync(session, ct);
        else
            await sessions.TouchAsync(session, ct);

        if (replies.Count > 0)
            await sessions.EnqueueAsync(session, replies, ct);

        var drained = await sessions.WaitAndDrainAsync(sessionId, lifespan, ct);
        if (ct.IsCancellationRequested)
        {
            await sessions.RequeueAsync(session, drained);
            if (!drained.IsEmpty)
            {
                MessengerMetrics.RecordPollRequeued();
                logger.LogInformation(
                    "MSNP gateway session {SessionId}: poll was cancelled after draining {Commands} commands and {Deliveries} deliveries, put them back",
                    sessionId, drained.Commands.Count, drained.Deliveries.Count);
            }

            ct.ThrowIfCancellationRequested();
        }

        waker.WakeSessionLater(sessionId);

        var delivered = TranslateDeliveries(session, drained.Deliveries);
        return new MsnpMessage([.. drained.Commands, .. delivered]);
    }

    private async Task<ChatSyncOutcome> SyncChatAsync(MsnpGatewaySession session, bool statusChanged, CancellationToken ct)
    {
        if (session.State != MsnpGatewaySessionState.Authenticated
            || session.ChatSuperseded
            || session.UserId is null
            || !MsnpPresenceStatus.TryParse(session.Status, out var status))
            return ChatSyncOutcome.Unchanged;

        if (!session.ChatRegistered)
            return await RegisterWithChatAsync(session, status, ct);

        EndpointStateResponse reply;
        try
        {
            reply = statusChanged
                ? await chatClient.SetPresenceAsync(
                    new SetPresenceRequest { EndpointId = session.SessionId, Status = status }, cancellationToken: ct)
                : await chatClient.ReportActivityAsync(
                    new ReportActivityRequest { EndpointId = session.SessionId }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not update Chat, presence may lag", session.SessionId);
            return ChatSyncOutcome.Unchanged;
        }

        switch (reply.State)
        {
            case EndpointState.Known:
                return ChatSyncOutcome.Unchanged;

            case EndpointState.Evicted:
                return ChatSyncOutcome.Evicted;

            case EndpointState.Superseded:
                logger.LogInformation("MSNP gateway session {SessionId}: a newer sign-in from the same device replaced it in Chat", session.SessionId);
                session.ChatSuperseded = true;
                return ChatSyncOutcome.SessionChanged;

            default:
                logger.LogInformation("MSNP gateway session {SessionId}: Chat no longer knows this endpoint, registering again", session.SessionId);
                return await RegisterWithChatAsync(session, status, ct);
        }
    }

    private async Task<ChatSyncOutcome> RegisterWithChatAsync(MsnpGatewaySession session, PresenceStatus status, CancellationToken ct)
    {
        var sessionTimeout = options.Value.ResolveSessionTtl(session.SessionTimeoutSeconds);
        var request = new RegisterEndpointRequest
        {
            EndpointId = session.SessionId,
            UserId = session.UserId,
            Address = session.Email,
            Status = status,
            SessionTimeoutSeconds = (int)sessionTimeout.TotalSeconds,
            InstanceId = session.EndpointId is { } epid ? ToInstanceId(epid) : "",
        };

        try
        {
            await chatClient.RegisterEndpointAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not register with Chat, will retry on the next request", session.SessionId);
            return ChatSyncOutcome.Unchanged;
        }

        session.ChatRegistered = true;

        if (session.EndpointId is { } deviceEpid && session.NotificationUri.Length > 0)
        {
            var device = new MsnpDevice(ToInstanceId(deviceEpid), session.NotificationUri, DateTimeOffset.UtcNow);
            await devices.RememberDeviceAsync(session.UserId!, device);
        }

        return ChatSyncOutcome.SessionChanged;
    }

    private async Task EndChatEndpointAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            await chatClient.EndEndpointAsync(new EndEndpointRequest { EndpointId = sessionId }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not end the Chat endpoint, it will expire instead", sessionId);
        }
    }

    private async Task<MsnpCommand?> RelayMessageAsync(MsnpGatewaySession session, MsnpCommand command, CancellationToken ct)
    {
        if (!MsnpSdg.TryRead(command, out var sdg))
            return VoidMessage(session, command, "unreadable", "its routing headers don't parse");

        if (!IsFromSession(session, sdg.From))
            return VoidMessage(session, command, "spoofed", "From isn't this session");

        if (sdg.To.Type != MsnpMuri.WindowsLiveType)
            return VoidMessage(session, command, "recipient_type", $"recipient type {sdg.To.Type} isn't relayed");

        if (ToMessageKind(sdg.MessageType) is not { } kind)
            return VoidMessage(session, command, "message_type", $"Message-Type {sdg.MessageType} isn't relayed");

        var p2pStep = kind == MessageKind.Data ? MsnpP2pStep.Describe(command.Payload) : (MsnpP2pStep?)null;
        if (p2pStep is { } step)
            MessengerMetrics.RecordP2pStep(step);

        var blp = kind == MessageKind.Data && MsnpBlpHeader.TryRead(command.Payload, out var header) ? header : (MsnpBlpHeader?)null;

        var maxBytes = kind == MessageKind.Data ? options.Value.MaxDataSdgBytes : options.Value.MaxTextSdgBytes;
        if (command.Payload!.Length > maxBytes)
        {
            MessengerMetrics.RecordSdgReceived(kind.ToString(), "too_big");
            logger.LogWarning("MSNP gateway session {SessionId}: SDG {TrId} is {Bytes} bytes, over the {Max} byte cap",
                session.SessionId, command.TrId, command.Payload.Length, maxBytes);
            return MsnpCommand.Error(MsnpErrorCode.Throttled, command.TrId);
        }

        var request = new SendMessageRequest
        {
            EndpointId = session.SessionId,
            TransactionId = command.TrId,
            RecipientAddress = sdg.To.Address,
            RecipientInstanceId = sdg.To.Epid is { } recipientEpid ? ToInstanceId(recipientEpid) : "",
            Kind = kind,
            Payload = ByteString.CopyFrom(command.Payload),
            StoreIfOffline = kind != MessageKind.Data && sdg.IsOfflineChannel,
        };

        SendMessageResponse response;
        try
        {
            response = await chatClient.SendMessageAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            MessengerMetrics.RecordSdgReceived(kind.ToString(), "chat_unavailable");
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not hand SDG {TrId} to Chat", session.SessionId, command.TrId);
            return MsnpCommand.Error(MsnpErrorCode.InternalServerError, command.TrId);
        }

        var reply = response.Outcome switch
        {
            SendOutcome.Delivered or SendOutcome.Voided or SendOutcome.Stored => null,
            SendOutcome.RecipientOffline => MsnpCommand.Error(OfflineErrorFor(sdg, kind), command.TrId),
            SendOutcome.Throttled => MsnpCommand.Error(MsnpErrorCode.Throttled, command.TrId),
            _ => MsnpCommand.Error(MsnpErrorCode.InternalServerError, command.TrId),
        };

        MessengerMetrics.RecordSdgReceived(kind.ToString(), response.Outcome.ToString());
        Activity.Current?.AddEvent(new ActivityEvent("msnp.sdg", tags: new ActivityTagsCollection
        {
            ["msnp.trid"] = command.TrId,
            ["msnp.message_type"] = kind.ToString(),
            ["msnp.bytes"] = command.Payload.Length,
            ["msnp.p2p_step"] = p2pStep?.ToString(),
            ["msnp.blp"] = blp?.ToString(),
            ["chat.outcome"] = response.Outcome.ToString(),
        }));

        logger.LogDebug(
            "MSNP gateway session {SessionId}: SDG {TrId} {Kind} to {Recipient} epid {Epid} ({Bytes} bytes, p2p {P2pStep}, blp {Blp}): {Outcome}, replied {Reply}",
            session.SessionId, command.TrId, kind, sdg.To.Address, sdg.To.Epid, command.Payload.Length, p2pStep, blp, response.Outcome,
            reply?.Verb ?? "nothing");
        return reply;
    }

    private MsnpCommand? VoidMessage(MsnpGatewaySession session, MsnpCommand command, string outcome, string reason)
    {
        MessengerMetrics.RecordSdgReceived("Other", outcome);
        logger.LogWarning("MSNP gateway session {SessionId}: dropping SDG {TrId}, {Reason}", session.SessionId, command.TrId, reason);
        return null;
    }

    private static bool IsFromSession(MsnpGatewaySession session, MsnpMuri from) =>
        from.Type == MsnpMuri.WindowsLiveType
        && string.Equals(from.Address, session.Email, StringComparison.OrdinalIgnoreCase)
        && session.EndpointId is not null
        && from.Epid == session.EndpointId;

    private static string ToInstanceId(Guid epid) => epid.ToString("D");

    private static MessageKind? ToMessageKind(string messageType) => messageType.ToLowerInvariant() switch
    {
        "text" => MessageKind.Text,
        "nudge" => MessageKind.Nudge,
        "data" => MessageKind.Data,
        _ => null,
    };

    private static MsnpErrorCode OfflineErrorFor(MsnpSdg sdg, MessageKind kind) =>
        kind == MessageKind.Data || sdg.IsOfflineChannel ? MsnpErrorCode.InvalidRecipient : MsnpErrorCode.RecipientNotOnline;

    private List<MsnpCommand> TranslateDeliveries(MsnpGatewaySession session, IReadOnlyList<ChatDelivery> deliveries)
    {
        var commands = new List<MsnpCommand>();
        foreach (var delivery in deliveries)
        {
            var queueTime = QueueTimeOf(delivery);
            using var activity = StartDeliveryActivity(delivery, queueTime);
            MessengerMetrics.RecordDeliveryWritten(DeliveryKindOf(delivery), queueTime);

            switch (delivery.KindCase)
            {
                case ChatDelivery.KindOneofCase.PresenceChanged:
                    commands.AddRange(TranslatePresence(session, delivery.PresenceChanged));
                    break;

                case ChatDelivery.KindOneofCase.MessageReceived:
                    var payload = PrepareMessagePayload(delivery.MessageReceived);
                    commands.Add(MsnpCommand.Create("SDG", "0").WithPayload(payload));
                    LogMessageDelivered(session, payload, queueTime);
                    break;
            }
        }

        return commands;
    }

    private static string DeliveryKindOf(ChatDelivery delivery) =>
        delivery.MessageReceived is { OriginalArrivalUnixMs: > 0 } ? "StoredMessage" : delivery.KindCase.ToString();

    private static byte[] PrepareMessagePayload(MessageReceived message)
    {
        var payload = message.Payload.ToByteArray();
        if (message.OriginalArrivalUnixMs <= 0)
            return MsnpSdg.WithOnlineChannel(payload);

        var arrival = DateTimeOffset.FromUnixTimeMilliseconds(message.OriginalArrivalUnixMs);
        return MsnpSdg.WithOriginalArrivalTime(payload, arrival);
    }

    private void LogMessageDelivered(MsnpGatewaySession session, byte[] payload, TimeSpan? queueTime)
    {
        if (!logger.IsEnabled(LogLevel.Debug) || !MsnpSdg.TryRead(MsnpCommand.Create("SDG", "0").WithPayload(payload), out var sdg))
            return;

        var isData = sdg.MessageType.Equals("Data", StringComparison.OrdinalIgnoreCase);
        var p2pStep = isData ? MsnpP2pStep.Describe(payload) : (MsnpP2pStep?)null;
        var blp = isData && MsnpBlpHeader.TryRead(payload, out var header) ? header : (MsnpBlpHeader?)null;
        logger.LogDebug(
            "MSNP gateway session {SessionId}: writing SDG {MessageType} from {Sender} ({Bytes} bytes, p2p {P2pStep}, blp {Blp}) after {QueueTime} queued",
            session.SessionId, sdg.MessageType, sdg.From.Address, payload.Length, p2pStep, blp, queueTime);
    }

    private static TimeSpan? QueueTimeOf(ChatDelivery delivery)
    {
        if (delivery.QueuedAtUnixMs <= 0)
            return null;

        var queuedAt = DateTimeOffset.FromUnixTimeMilliseconds(delivery.QueuedAtUnixMs);
        var waited = DateTimeOffset.UtcNow - queuedAt;
        return waited < TimeSpan.Zero ? TimeSpan.Zero : waited;
    }

    private static Activity? StartDeliveryActivity(ChatDelivery delivery, TimeSpan? queueTime)
    {
        ActivityLink[] links = ActivityContext.TryParse(delivery.TraceParent, null, out var sender)
            ? [new ActivityLink(sender)]
            : [];

        var activity = ServiceTelemetry.ActivitySource.StartActivity(
            "msnp.deliver", ActivityKind.Internal, parentContext: default, links: links);

        activity?.SetTag("msnp.delivery_kind", delivery.KindCase.ToString());
        activity?.SetTag("msnp.queue_time_ms", queueTime?.TotalMilliseconds);
        return activity;
    }

    private static IEnumerable<MsnpCommand> TranslatePresence(MsnpGatewaySession session, PresenceChanged change)
    {
        if (string.Equals(change.Address, session.Email, StringComparison.OrdinalIgnoreCase))
            yield break;

        var to = MsnpMuri.ForWindowsLive(session.Email);
        var from = MsnpMuri.ForWindowsLive(change.Address);
        if (change.Status == PresenceStatus.Offline)
        {
            yield return MsnpNotifications.PresenceRemovedNfy(to, from);
            yield break;
        }

        foreach (var removed in ParseInstanceIds(change.RemovedInstanceIds))
            yield return MsnpNotifications.EndpointRemovedNfy(to, from, removed);

        var document = MsnpNotifications.PresenceDocument(change.Status, ParseInstanceIds(change.InstanceIds));
        yield return MsnpNotifications.PresenceNfy(to, from, document);
    }

    private static IEnumerable<Guid> ParseInstanceIds(IEnumerable<string> instanceIds)
    {
        foreach (var instanceId in instanceIds)
        {
            if (MsnpMuri.TryParseEpid(instanceId, out var epid))
                yield return epid;
        }
    }

    private bool HandlePut(MsnpGatewaySession session, MsnpCommand put)
    {
        if (put.Payload is not { Length: > 0 })
            return false;

        var body = MsnpLayeredBody.Parse(put.PayloadText);

        if (!body.Headers.TryGetValue("Uri", out var uri) || !uri.Equals("/user", StringComparison.OrdinalIgnoreCase))
            return false;

        if (body.Headers.TryGetValue("From", out var from))
            session.EndpointId = MsnpMuri.TryParse(from, out var self) ? self.Epid : null;

        var previousStatus = session.Status;
        if (body.Headers.TryGetValue("Content-Type", out var contentType)
            && contentType.StartsWith("application/user+xml", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var xml = XElement.Parse(body.Content);
                session.Status = xml.Element("s")?.Element("Status")?.Value ?? session.Status;
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "MSNP gateway session {SessionId}: malformed self-presence XML", session.SessionId);
            }
        }

        logger.LogInformation(
            "MSNP gateway session {SessionId} presence: user {UserId} status={Status} endpoint={EndpointId} puid={Puid}",
            session.SessionId, session.UserId, session.Status, session.EndpointId, session.Puid);

        return !string.Equals(previousStatus, session.Status, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<MsnpMessage> RejectSignInAsync(
        MsnpGatewaySession session, MsnpCommand usr, SsoTicketVerification verification, CancellationToken ct)
    {
        await sessions.DeleteAsync(session.SessionId, ct);

        var code = verification == SsoTicketVerification.Unavailable
            ? MsnpErrorCode.InternalServerError
            : MsnpErrorCode.AuthenticationFailed;

        logger.LogWarning("MSNP gateway session {SessionId}: sign-in refused with {Code}", session.SessionId, (int)code);

        return MsnpMessage.Of(MsnpCommand.Error(code, usr.TrId));
    }

    private async Task<SsoTicketVerification> VerifySsoTicketAsync(MsnpGatewaySession session, MsnpCommand usr, CancellationToken ct)
    {
        var ticketArg = usr.Arguments.FirstOrDefault(a => a.StartsWith("t=", StringComparison.OrdinalIgnoreCase));
        var ticket = ticketArg?[2..];
        if (ticket is { Length: > 0 } && ticket.IndexOf('&') is var amp and >= 0)
            ticket = ticket[..amp];

        if (string.IsNullOrEmpty(ticket))
        {
            logger.LogWarning("MSNP gateway session {SessionId}: second USR carried no t= ticket", session.SessionId);
            MessengerMetrics.RecordSsoVerification("no_ticket");
            return SsoTicketVerification.Rejected;
        }

        var request = new VerifyTokenRequest { Token = ticket, TokenType = "JWT" };
        request.ServiceTargets.AddRange(ServiceTargets);

        VerifyResponse reply;
        try
        {
            reply = await authenticationClient.VerifySecurityTokenAsync(request, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not reach AuthenticationService to verify SSO ticket", session.SessionId);
            MessengerMetrics.RecordSsoVerification("backend_unreachable");
            return SsoTicketVerification.Unavailable;
        }

        if (reply.Code != 0)
        {
            logger.LogWarning("MSNP gateway session {SessionId}: SSO ticket failed verification (code={Code:X})", session.SessionId, reply.Code);
            MessengerMetrics.RecordSsoVerification("rejected");
            return SsoTicketVerification.Rejected;
        }

        MessengerMetrics.RecordSsoVerification("ok");
        session.UserId = reply.Id;
        var claims = reply.Claims.ToDictionary(c => c.Type, c => c.Value);
        if (claims.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
            session.Email = email;

        session.Cid = claims.GetValueOrDefault("cid");
        session.Puid = claims.TryGetValue("puid", out var puidHex)
            && long.TryParse(puidHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var puid)
                ? puid
                : null;

        logger.LogInformation("MSNP gateway session {SessionId} SSO ticket verified: user {UserId} (puid={Puid} cid={Cid})",
            session.SessionId, session.UserId, session.Puid, session.Cid);
        return SsoTicketVerification.Verified;
    }

    public async Task<bool> PushPresenceAsync(
        string sessionId, MsnpMuri fromMuri, PresenceStatus status, MsnpNotifType notifType, CancellationToken ct)
    {
        var session = await sessions.FindAsync(sessionId, ct);
        if (session is null)
            return false;

        var document = MsnpNotifications.PresenceDocument(status, []);
        var nfy = MsnpNotifications.PresenceNfy(MsnpMuri.ForWindowsLive(session.Email), fromMuri, document, notifType);
        await sessions.EnqueueAsync(session, [nfy], ct);
        waker.WakeSessionLater(sessionId);

        logger.LogInformation(
            "MSNP gateway session {SessionId} pushed presence NFY: from={From} status={Status} notifType={NotifType}",
            sessionId, fromMuri, status, notifType);
        return true;
    }

    public async Task CloseAsync(string sessionId, CancellationToken ct)
    {
        if (await sessions.FindAsync(sessionId, ct) is { } session)
            await SignOutAsync(session, ct);
        else
            await sessions.DeleteAsync(sessionId, ct);
    }

    private async Task SignOutAsync(MsnpGatewaySession session, CancellationToken ct)
    {
        if (session.ChatRegistered)
            await EndChatEndpointAsync(session.SessionId, ct);

        if (session is { UserId: not null, EndpointId: { } epid })
            await devices.ForgetDeviceAsync(session.UserId, ToInstanceId(epid));

        await sessions.DeleteAsync(session.SessionId, ct);
    }

    private static MsnpCommand? Find(MsnpMessage message, string verb) =>
        message.Commands.FirstOrDefault(c => c.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase));
}
