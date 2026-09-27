using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReLiveWP.Services.Docs;

// dav requests fan out to the skydocs backend and then to the provider, neither of which should
// surface as an unhandled 500 to the device
public sealed class UpstreamFaultFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<UpstreamFaultFilter>>();

        context.Result = context.Exception switch
        {
            RpcException { StatusCode: StatusCode.NotFound } => new NotFoundResult(),
            RpcException { StatusCode: StatusCode.Unauthenticated } => new UnauthorizedResult(),
            RpcException { StatusCode: StatusCode.PermissionDenied } => new ForbidResult(),
            RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded } rpc
                => Fault(StatusCodes.Status503ServiceUnavailable, "SkyDocs backend unavailable", rpc),
            RpcException rpc => Fault(StatusCodes.Status502BadGateway, "SkyDocs backend fault", rpc),
            HttpRequestException http => Fault(StatusCodes.Status502BadGateway, "Provider content fetch failed", http),
            _ => null,
        };

        context.ExceptionHandled = context.Result is not null;

        StatusCodeResult Fault(int statusCode, string message, Exception exception)
        {
            logger.LogWarning(exception, "{Message} for {Method} {Path}", message,
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return new StatusCodeResult(statusCode);
        }
    }
}
