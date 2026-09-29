using System.Text;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Messenger.Http;
using ReLiveWP.Services.Messenger.Msnp;
using ReLiveWP.Services.Messenger.Services;

namespace ReLiveWP.Services.Messenger.Endpoints;

public static class MsnpGatewayEndpoints
{
    private const string DefaultGatewayHost = "gateway.messenger.relivewp.net";
    private const int MaxPollLifespanSeconds = 180;

    public static IEndpointRouteBuilder MapMsnpGateway(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/gateway/gateway.dll", ["GET", "POST"], HandleAsync);
#if DEBUG
        app.MapPost("/gateway/debug/presence", DebugPushPresenceAsync);
#endif
        return app;
    }

#if DEBUG
    // helper to manually trip a presence
    private static async Task<IResult> DebugPushPresenceAsync(HttpContext context, MsnpGatewayService gateway, CancellationToken ct)
    {
        var query = context.Request.Query;
        var sessionId = query["sessionId"].ToString();
        var from = query["from"].ToString();
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(from))
            return Results.BadRequest("sessionId and from are required.");

        if (!MsnpMuri.TryParse(from, out var fromMuri))
            return Results.BadRequest("from isn't a MURI.");

        var statusValue = query["status"].ToString() is { Length: > 0 } s ? s : "NLN";
        if (!MsnpPresenceStatus.TryParse(statusValue, out var status))
            return Results.BadRequest("status isn't a presence code.");

        var notifTypeValue = query["notifType"].ToString() is { Length: > 0 } n ? n : "Full";
        if (!Enum.TryParse<MsnpNotifType>(notifTypeValue, ignoreCase: true, out var notifType))
            return Results.BadRequest("notifType must be Full or Partial.");

        var pushed = await gateway.PushPresenceAsync(sessionId, fromMuri, status, notifType, ct);
        return pushed ? Results.Ok($"pushed {statusValue} for {from}") : Results.NotFound("unknown session");
    }
#endif

    private static async Task<IResult> HandleAsync(
        HttpContext context, MsnpGatewayService gateway, IConfiguration config, IOptions<MessengerOptions> options,
        ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var maxCommands = options.Value.MaxCommandsPerRequest;
        var logger = loggerFactory.CreateLogger("MsnpGateway");
        var query = context.Request.Query;

        byte[] rawBody = [];
        if (context.Request.ContentLength is > 0 || context.Request.Method == HttpMethods.Post)
        {
            using var buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer, ct);
            rawBody = buffer.ToArray();
        }

        var actionValue = query["Action"].ToString();
        var sessionIdValue = query["SessionID"].ToString();

        logger.LogInformation("Gateway {Method} Action={Action} SessionID={SessionId} ({Length} bytes)",
            context.Request.Method, actionValue, sessionIdValue, rawBody.Length);
        LogRawBody(logger, rawBody);

        MsnpGatewayAction action;
        if (string.IsNullOrEmpty(actionValue))
        {
            if (string.IsNullOrEmpty(sessionIdValue))
            {
                logger.LogWarning("Rejecting gateway request: unknown/missing Action '{Action}'", actionValue);
                MessengerMetrics.RecordGatewayRequest("unknown", "bad_request");
                return Results.BadRequest("Unknown or missing Action.");
            }

            action = MsnpGatewayAction.Poll;
        }
        else if (!Enum.TryParse(actionValue, ignoreCase: true, out action))
        {
            logger.LogWarning("Rejecting gateway request: unknown/missing Action '{Action}'", actionValue);
            MessengerMetrics.RecordGatewayRequest("unknown", "bad_request");
            return Results.BadRequest("Unknown or missing Action.");
        }


        var configuredHost = config["Messenger:GatewayHost"];
        var requestHost = context.Request.Host.Host;

        // the device will try to reconnect to GW-IP if it's changed, we can abuse this for
        // load balancing if we need to
        var gwIp = !string.IsNullOrEmpty(configuredHost) ? configuredHost
                 : IsLoopbackHost(requestHost) ? DefaultGatewayHost
                 : requestHost;
        var sessionId = sessionIdValue;


        logger.LogInformation("Echoing GW-IP={GwIp} (request Host {RequestHost}) for {Action}", gwIp, requestHost, action);

        var actionName = action.ToString();
        switch (action)
        {
            case MsnpGatewayAction.Open:
                {
                    if (!TryParseBody(logger, rawBody, maxCommands, out var message))
                    {
                        MessengerMetrics.RecordGatewayRequest(actionName, "bad_request");
                        return Results.BadRequest("Malformed MSNP body.");
                    }

                    var notificationUri = query["NotificationURI"].ToString();
                    int? sessionTimeout = int.TryParse(query["SessionTimeout"], out var t) ? t : null;

                    var (openSessionId, reply) = await gateway.OpenAsync(message, notificationUri, sessionTimeout, ct);
                    MessengerMetrics.RecordGatewayRequest(actionName, "ok");
                    return MsnpGatewayResult.Open(openSessionId, gwIp, moreData: false, reply);
                }

            case MsnpGatewayAction.Poll:
                {
                    if (string.IsNullOrEmpty(sessionId))
                    {
                        MessengerMetrics.RecordGatewayRequest(actionName, "bad_request");
                        return Results.BadRequest("Missing SessionID.");
                    }

                    if (!TryParseBody(logger, rawBody, maxCommands, out var pollMessage))
                    {
                        MessengerMetrics.RecordGatewayRequest(actionName, "bad_request");
                        return Results.BadRequest("Malformed MSNP body.");
                    }

                    var lifespan = int.TryParse(query["Lifespan"], out var ls) && ls > 0
                        ? TimeSpan.FromSeconds(Math.Min(ls, MaxPollLifespanSeconds))
                        : TimeSpan.Zero;

                    var reply = await gateway.PollAsync(sessionId, pollMessage, lifespan, ct);
                    MessengerMetrics.RecordGatewayRequest(actionName, reply is not null ? "ok" : "session_closed");
                    return reply is not null
                        ? MsnpGatewayResult.Poll(sessionId, gwIp, moreData: false, reply)
                        : MsnpGatewayResult.SessionClosed(sessionId);
                }

            case MsnpGatewayAction.Close:
                {
                    if (string.IsNullOrEmpty(sessionId))
                    {
                        MessengerMetrics.RecordGatewayRequest(actionName, "bad_request");
                        return Results.BadRequest("Missing SessionID.");
                    }

                    await gateway.CloseAsync(sessionId, ct);
                    MessengerMetrics.RecordGatewayRequest(actionName, "ok");
                    return Results.Ok();
                }

            default:
                MessengerMetrics.RecordGatewayRequest(actionName, "bad_request");
                return Results.BadRequest("Unknown Action.");
        }
    }

    private static bool TryParseBody(ILogger logger, byte[] rawBody, int maxCommands, out MsnpMessage message)
    {
        if (!MsnpMessage.TryParse(rawBody, maxCommands, out message))
        {
            logger.LogWarning("Rejecting MSNP body ({Length} bytes) that's malformed or over {Max} commands, raw body is logged at Trace",
                rawBody.Length, maxCommands);
            return false;
        }

        if (message.Commands.Count > 0)
            logger.LogDebug("Gateway commands:\n{Commands}",
                string.Join('\n', message.Commands.Select(c => c.ToLogString())));

        return true;
    }

    // tickets and message text live in here, so it stays at Trace
    private static void LogRawBody(ILogger logger, byte[] rawBody)
    {
        if (rawBody.Length > 0 && logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("Gateway raw body:\n{Body}", Encoding.UTF8.GetString(rawBody));
    }

    private static bool IsLoopbackHost(string host) =>
        string.IsNullOrEmpty(host)
        || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (System.Net.IPAddress.TryParse(host, out var ip) && System.Net.IPAddress.IsLoopback(ip));
}
