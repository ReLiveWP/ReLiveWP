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
        app.MapPost("/gateway/debug/presence", DebugPushPresenceAsync);
        return app;
    }

    // helper to manually trip a presence
    private static async Task<IResult> DebugPushPresenceAsync(HttpContext context, MsnpGatewayService gateway, CancellationToken ct)
    {
        var query = context.Request.Query;
        var sessionId = query["sessionId"].ToString();
        var from = query["from"].ToString();
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(from))
            return Results.BadRequest("sessionId and from are required.");

        var status = query["status"].ToString() is { Length: > 0 } s ? s : "NLN";
        var notifType = query["notifType"].ToString() is { Length: > 0 } n ? n : "Full";
        var name = query["name"].ToString() is { Length: > 0 } fn ? fn : null;

        var pushed = await gateway.PushPresenceAsync(sessionId, from, status, name, notifType, ct);
        return pushed ? Results.Ok($"pushed {status} for {from}") : Results.NotFound("unknown session");
    }

    private static async Task<IResult> HandleAsync(HttpContext context, MsnpGatewayService gateway, IConfiguration config, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("MsnpGateway");
        var query = context.Request.Query;

        string rawBody = "";
        if (context.Request.ContentLength is > 0 || context.Request.Method == HttpMethods.Post)
        {
            using var reader = new StreamReader(context.Request.Body);
            rawBody = await reader.ReadToEndAsync(ct);
        }

        logger.LogInformation("Gateway {Method} {Query}\n{Body}",
            context.Request.Method, context.Request.QueryString.Value, rawBody);

        var actionValue = query["Action"].ToString();
        var sessionIdValue = query["SessionID"].ToString();

        MsnpGatewayAction action;
        if (string.IsNullOrEmpty(actionValue))
        {
            if (string.IsNullOrEmpty(sessionIdValue))
            {
                logger.LogWarning("Rejecting gateway request: unknown/missing Action '{Action}'", actionValue);
                return Results.BadRequest("Unknown or missing Action.");
            }

            action = MsnpGatewayAction.Poll;
        }
        else if (!Enum.TryParse(actionValue, ignoreCase: true, out action))
        {
            logger.LogWarning("Rejecting gateway request: unknown/missing Action '{Action}'", actionValue);
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

        switch (action)
        {
            case MsnpGatewayAction.Open:
            {
                if (!MsnpMessage.TryParse(rawBody, out var message))
                {
                    logger.LogWarning("Rejecting open: malformed MSNP body:\n{Body}", rawBody);
                    return Results.BadRequest("Malformed MSNP body.");
                }

                var notificationUri = query["NotificationURI"].ToString();
                int? sessionTimeout = int.TryParse(query["SessionTimeout"], out var t) ? t : null;

                var (openSessionId, reply) = await gateway.OpenAsync(message, notificationUri, sessionTimeout, ct);
                return MsnpGatewayResult.Open(openSessionId, gwIp, moreData: false, reply);
            }

            case MsnpGatewayAction.Poll:
            {
                if (string.IsNullOrEmpty(sessionId))
                    return Results.BadRequest("Missing SessionID.");

                if (!MsnpMessage.TryParse(rawBody, out var pollMessage))
                {
                    logger.LogWarning("Rejecting poll: malformed MSNP body:\n{Body}", rawBody);
                    return Results.BadRequest("Malformed MSNP body.");
                }

                var lifespan = int.TryParse(query["Lifespan"], out var ls) && ls > 0
                    ? TimeSpan.FromSeconds(Math.Min(ls, MaxPollLifespanSeconds))
                    : TimeSpan.Zero;

                var reply = await gateway.PollAsync(sessionId, pollMessage, lifespan, ct);
                return reply is not null
                    ? MsnpGatewayResult.Poll(sessionId, gwIp, moreData: false, reply)
                    : MsnpGatewayResult.SessionClosed(sessionId);
            }

            case MsnpGatewayAction.Close:
            {
                if (string.IsNullOrEmpty(sessionId))
                    return Results.BadRequest("Missing SessionID.");

                await gateway.CloseAsync(sessionId, ct);
                return Results.Ok();
            }

            default:
                return Results.BadRequest("Unknown Action.");
        }
    }

    private static bool IsLoopbackHost(string host) =>
        string.IsNullOrEmpty(host)
        || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (System.Net.IPAddress.TryParse(host, out var ip) && System.Net.IPAddress.IsLoopback(ip));
}
