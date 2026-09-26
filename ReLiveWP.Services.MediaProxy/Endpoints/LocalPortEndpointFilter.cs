namespace ReLiveWP.Services.MediaProxy.Endpoints;

public sealed class LocalPortEndpointFilter(int port) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Connection.LocalPort != port)
            return ValueTask.FromResult<object?>(TypedResults.NotFound());

        return next(context);
    }
}
