using System.Diagnostics;
using System.Diagnostics.Metrics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ReLiveWP.ServiceDefaults;

public sealed class GrpcTelemetryInterceptor : Interceptor
{
    private static readonly Histogram<double> ServerCallDuration = ServiceTelemetry.Meter.CreateHistogram<double>(
        "rpc.server.call.duration", "s", advice: ServiceTelemetry.RequestSeconds);

    private static readonly Histogram<double> ClientCallDuration = ServiceTelemetry.Meter.CreateHistogram<double>(
        "rpc.client.call.duration", "s", advice: ServiceTelemetry.RequestSeconds);

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) =>
        ServerCallAsync(context, () => continuation(request, context));

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation) =>
        ServerCallAsync(context, () => continuation(requestStream, context));

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation) =>
        ServerCallAsync(context, () => continuation(request, responseStream, context));

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) =>
        ServerCallAsync(context, () => continuation(requestStream, responseStream, context));

    private static Task ServerCallAsync(ServerCallContext context, Func<Task> call) =>
        ServerCallAsync(context, async () =>
        {
            await call();
            return true;
        });

    private static async Task<T> ServerCallAsync<T>(ServerCallContext context, Func<Task<T>> call)
    {
        var started = Stopwatch.GetTimestamp();
        var status = StatusCode.Unknown;
        try
        {
            var response = await call();
            status = context.Status.StatusCode;
            return response;
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            status = StatusCode.Cancelled;
            throw;
        }
        finally
        {
            var (service, method) = SplitFullName(context.Method);
            Record(ServerCallDuration, service, method, started, status);

            if (Activity.Current is { } activity)
            {
                activity.SetTag("rpc.grpc.status_code", (int)status);
                if (status != StatusCode.OK)
                    activity.SetStatus(ActivityStatusCode.Error, status.ToString());
            }
        }
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var status = StatusCode.Unknown;
        try
        {
            var response = continuation(request, context);
            status = StatusCode.OK;
            return response;
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            Record(ClientCallDuration, context.Method.ServiceName, context.Method.Name, started, status);
        }
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(request, context);
        return new AsyncUnaryCall<TResponse>(
            ObserveResponseAsync(call.ResponseAsync, call.GetStatus, context.Method, started),
            call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(context);
        return new AsyncClientStreamingCall<TRequest, TResponse>(
            call.RequestStream,
            ObserveResponseAsync(call.ResponseAsync, call.GetStatus, context.Method, started),
            call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(request, context);
        return new AsyncServerStreamingCall<TResponse>(
            call.ResponseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers,
            ObserveOnDispose(call.GetStatus, call.Dispose, context.Method, started));
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(context);
        return new AsyncDuplexStreamingCall<TRequest, TResponse>(
            call.RequestStream, call.ResponseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers,
            ObserveOnDispose(call.GetStatus, call.Dispose, context.Method, started));
    }

    private static async Task<TResponse> ObserveResponseAsync<TResponse>(
        Task<TResponse> responseTask, Func<Status> getStatus, IMethod method, long started)
    {
        var status = StatusCode.Unknown;
        try
        {
            var response = await responseTask;
            status = getStatus().StatusCode;
            return response;
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        catch (OperationCanceledException)
        {
            status = StatusCode.Cancelled;
            throw;
        }
        finally
        {
            Record(ClientCallDuration, method.ServiceName, method.Name, started, status);
        }
    }

    // streaming calls have no single completion task, so the call is measured until the caller disposes it.
    // a call disposed before the server finished has no status yet, that is what Cancelled means here
    private static Action ObserveOnDispose(Func<Status> getStatus, Action dispose, IMethod method, long started)
    {
        var recorded = false;
        return () =>
        {
            if (!recorded)
            {
                recorded = true;
                var status = StatusCode.Cancelled;
                try
                {
                    status = getStatus().StatusCode;
                }
                catch (InvalidOperationException)
                {
                }

                Record(ClientCallDuration, method.ServiceName, method.Name, started, status);
            }

            dispose();
        };
    }

    private static void Record(Histogram<double> histogram, string service, string method, long started, StatusCode status)
    {
        var tags = new TagList
        {
            { "rpc.system", "grpc" },
            { "rpc.service", service },
            { "rpc.method", method },
            { "rpc.grpc.status_code", (int)status },
        };

        histogram.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
    }

    private static (string Service, string Method) SplitFullName(string fullName)
    {
        var slash = fullName.LastIndexOf('/');
        if (slash < 0)
            return (fullName, string.Empty);

        return (fullName[..slash].TrimStart('/'), fullName[(slash + 1)..]);
    }
}
