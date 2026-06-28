using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ReLiveWP.Services.Login;

public class AuthForwardingInterceptor(IHttpContextAccessor httpContextAccessor) : Interceptor
{
    private CallOptions WithAuth(CallOptions options)
    {
        var auth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(auth)) 
            return options;

        Metadata merged = options.Headers != null ? [..options.Headers] : [];
        merged.Add(new Metadata.Entry("Authorization", auth));
        return options.WithHeaders(merged);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, WithAuth(context.Options)));

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, WithAuth(context.Options)));
}
