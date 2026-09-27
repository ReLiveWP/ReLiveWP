using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReLiveWP.Services.Hub;

public sealed class GrpcFaultFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RpcException rpc) return;

        context.Result = rpc.StatusCode switch
        {
            StatusCode.NotFound or StatusCode.InvalidArgument or StatusCode.FailedPrecondition
                => new BadRequestObjectResult(rpc.Status.Detail),

            StatusCode.PermissionDenied => new ForbidResult(),
            StatusCode.Unauthenticated => new UnauthorizedResult(),
            StatusCode.Unavailable or StatusCode.Unimplemented
                => new ObjectResult(rpc.Status.Detail) { StatusCode = StatusCodes.Status503ServiceUnavailable },

            _ => null!,
        };

        context.ExceptionHandled = context.Result is not null;
    }
}
