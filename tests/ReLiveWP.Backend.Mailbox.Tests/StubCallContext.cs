using Grpc.Core;

namespace ReLiveWP.Backend.Mailbox.Tests;

// minimal ServerCallContext for calling RPCs without hosting a gRPC server; only CancellationToken is
// wired up. Grpc.Core.Testing.TestServerCallContext would do this properly but drags in the
// discontinued native Grpc.Core runtime, not worth it for one field.
internal sealed class StubCallContext : ServerCallContext
{
    protected override string MethodCore => "test";
    protected override string HostCore => "test";
    protected override string PeerCore => "test";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => [];
    protected override CancellationToken CancellationTokenCore => default;
    protected override Metadata ResponseTrailersCore => [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());
    protected override IDictionary<object, object> UserStateCore => new Dictionary<object, object>();

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
        Task.CompletedTask;
}
