using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Grpc.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Services;

public class MsnpGatewayService(MsnpGatewaySessionStore sessions, Authentication.AuthenticationClient authenticationClient, ILogger<MsnpGatewayService> logger)
{
    private const string SsoPolicy = "MBI_KEY_OLD";
    private static readonly string[] ServiceTargets = ["messengerclear.live.com", "messengerclear.live-int.com"];

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
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var email = usr is { Arguments.Length: > 0 } ? usr.Arguments[^1] : "";

        if (usr is not null)
            replies.Add(MsnpCommand.Create("USR", usr.TrId, "SSO", "S", SsoPolicy, nonce));

        await sessions.CreateAsync(new MsnpGatewaySession
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

        logger.LogInformation("MSNP gateway open: session {SessionId} for {Email}", sessionId, email);

        return (sessionId, new MsnpMessage(replies));
    }

    public async Task<MsnpMessage?> PollAsync(string sessionId, MsnpMessage request, TimeSpan lifespan, CancellationToken ct)
    {
        var session = await sessions.FindAsync(sessionId, ct);
        if (session is null)
            return null;


        if (Find(request, "OUT") is not null)
        {
            await sessions.DeleteAsync(sessionId, ct);
            logger.LogInformation("MSNP gateway session {SessionId} closed by client OUT", sessionId);
            return null;
        }

        var replies = new List<MsnpCommand>();
        var sessionDirty = false;

        foreach (var command in request.Commands)
        {
            switch (command.Verb.ToUpperInvariant())
            {
                case "USR" when session.State == MsnpGatewaySessionState.AwaitingSsoTicket
                    && command.Arguments.Length > 1
                    && command.Arguments[0].Equals("SSO", StringComparison.OrdinalIgnoreCase):
                    {
                        await VerifySsoTicketAsync(session, command, ct);
                        session.State = MsnpGatewaySessionState.Authenticated;
                        sessionDirty = true;
                        logger.LogInformation("MSNP gateway session {SessionId} authenticated for {Email}", sessionId, session.Email);
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
                        replies.Add(MsnpCommand.Create("CHL", command.TrId));
                        break;
                    }
                case "PUT":
                    {
                        HandlePut(session, command);
                        sessionDirty = true;
                        replies.Add(MsnpCommand.Create("PUT", command.TrId, "OK", "0"));
                        break;
                    }
            }
        }

        if (sessionDirty)
            await sessions.SaveAsync(session, ct);
        else
            await sessions.TouchAsync(sessionId, ct);

        if (replies.Count > 0)
            await sessions.EnqueueAsync(sessionId, replies, ct);

        var pending = await sessions.WaitAndDrainAsync(sessionId, lifespan, ct);
        return new MsnpMessage(pending);
    }

    private void HandlePut(MsnpGatewaySession session, MsnpCommand put)
    {
        if (put.Payload is not { Length: > 0 } payload)
            return;

        var body = MsnpLayeredBody.Parse(payload);

        if (!body.Headers.TryGetValue("Uri", out var uri) || !uri.Equals("/user", StringComparison.OrdinalIgnoreCase))
            return;

        if (body.Headers.TryGetValue("From", out var from))
            session.EndpointId = ParseEpid(from);

        if (body.Headers.TryGetValue("Content-Type", out var contentType)
            && contentType.StartsWith("application/user+xml", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var xml = XElement.Parse(body.Content);
                session.Status = xml.Element("s")?.Element("Status")?.Value;
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "MSNP gateway session {SessionId}: malformed self-presence XML", session.SessionId);
            }
        }

        logger.LogInformation(
            "MSNP gateway session {SessionId} presence: {Email} status={Status} endpoint={EndpointId} puid={Puid}",
            session.SessionId, session.Email, session.Status, session.EndpointId, session.Puid);
    }
    private async Task VerifySsoTicketAsync(MsnpGatewaySession session, MsnpCommand usr, CancellationToken ct)
    {
        var ticketArg = usr.Arguments.FirstOrDefault(a => a.StartsWith("t=", StringComparison.OrdinalIgnoreCase));
        var ticket = ticketArg?[2..];
        if (ticket is { Length: > 0 } && ticket.IndexOf('&') is var amp and >= 0)
            ticket = ticket[..amp];

        if (string.IsNullOrEmpty(ticket))
        {
            logger.LogWarning("MSNP gateway session {SessionId}: second USR carried no t= ticket", session.SessionId);
            return;
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
            // TODO: we need to figure out how to reject bad sessions
            logger.LogWarning(ex, "MSNP gateway session {SessionId}: could not reach AuthenticationService to verify SSO ticket", session.SessionId);
            return;
        }

        if (reply.Code != 0)
        {
            // TODO: ditto
            logger.LogWarning("MSNP gateway session {SessionId}: SSO ticket failed verification (code={Code:X})", session.SessionId, reply.Code);
            return;
        }

        var claims = reply.Claims.ToDictionary(c => c.Type, c => c.Value);
        if (claims.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
            session.Email = email;

        session.Cid = claims.GetValueOrDefault("cid");
        session.Puid = claims.TryGetValue("puid", out var puidHex)
            && long.TryParse(puidHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var puid)
                ? puid
                : null;

        logger.LogInformation("MSNP gateway session {SessionId} SSO ticket verified: {Email} (puid={Puid} cid={Cid})",
            session.SessionId, session.Email, session.Puid, session.Cid);
    }

    private static string? ParseEpid(string muri)
    {
        var semi = muri.IndexOf(';');
        if (semi < 0)
            return null;

        foreach (var part in muri[(semi + 1)..].Split(';'))
        {
            var eq = part.IndexOf('=');
            if (eq >= 0 && part[..eq].Equals("epid", StringComparison.OrdinalIgnoreCase))
                return part[(eq + 1)..].Trim('{', '}');
        }

        return null;
    }

    public async Task<bool> PushPresenceAsync(
        string sessionId, string fromMuri, string status, string? friendlyName, string notifType, CancellationToken ct)
    {
        var session = await sessions.FindAsync(sessionId, ct);
        if (session is null)
            return false;

        var xml = MsnpNotifications.PresenceDocument(status, friendlyName);
        var nfy = MsnpNotifications.PresenceNfy(session.Email, toEpid: null, fromMuri, xml, notifType);
        await sessions.EnqueueAsync(sessionId, [nfy], ct);

        logger.LogInformation(
            "MSNP gateway session {SessionId} pushed presence NFY: from={From} status={Status} notifType={NotifType}",
            sessionId, fromMuri, status, notifType);
        return true;
    }

    public Task CloseAsync(string sessionId, CancellationToken ct) =>
        sessions.DeleteAsync(sessionId, ct);

    private static MsnpCommand? Find(MsnpMessage message, string verb) =>
        message.Commands.FirstOrDefault(c => c.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase));
}
