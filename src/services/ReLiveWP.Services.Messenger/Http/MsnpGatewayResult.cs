using ReLiveWP.Services.Messenger.Msnp;

namespace ReLiveWP.Services.Messenger.Http;

public class MsnpGatewayResult : IResult
{
    private readonly string headerValue;
    private readonly MsnpMessage body;

    private MsnpGatewayResult(string headerValue, MsnpMessage body)
    {
        this.headerValue = headerValue;
        this.body = body;
    }

    public static MsnpGatewayResult Open(string sessionId, string gwIp, bool moreData, MsnpMessage body, string serviceChannel = "NS") =>
        new($"SessionID={sessionId}; GW-IP={gwIp}; MoreData={(moreData ? "true" : "false")}; Service-Channel={serviceChannel}", body);

    public static MsnpGatewayResult Poll(string sessionId, string gwIp, bool moreData, MsnpMessage body) =>
        new($"SessionID={sessionId}; GW-IP={gwIp}; MoreData={(moreData ? "true" : "false")}", body);

    public static MsnpGatewayResult SessionClosed(string sessionId) =>
        new($"SessionID={sessionId}; Session=close", MsnpMessage.Of());

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers["X-MSN-Messenger"] = headerValue;
        httpContext.Response.ContentType = "text/plain";
        return httpContext.Response.WriteAsync(body.Serialize());
    }
}
