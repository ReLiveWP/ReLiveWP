using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

// minimal ServerCallContext for calling RPCs without hosting a gRPC server. GetHttpContext() reads
// the "__HttpContext" user-state key, so the service's GetUserId works off the principal we put there.
internal sealed class StubCallContext : ServerCallContext
{
    private readonly Dictionary<object, object> userState;

    public StubCallContext(Guid userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test")),
        };

        userState = new Dictionary<object, object> { ["__HttpContext"] = httpContext };
    }

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
    protected override IDictionary<object, object> UserStateCore => userState;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
        Task.CompletedTask;
}
