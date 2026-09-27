using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace ReLiveWP.Identity.Grpc;

public class AuthForwardingInterceptor(IHttpContextAccessor httpContextAccessor) : Interceptor
{
    private CallOptions WithAuth(CallOptions options)
    {
        var auth = httpContextAccessor.HttpContext?.User?.Id();
        if (string.IsNullOrWhiteSpace(auth)) 
            return options;

        Metadata merged = options.Headers != null ? [..options.Headers] : [];
        merged.Add(new Metadata.Entry("X-User-Id", auth));
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

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, WithAuth(context.Options)));

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, WithAuth(context.Options)));
}
