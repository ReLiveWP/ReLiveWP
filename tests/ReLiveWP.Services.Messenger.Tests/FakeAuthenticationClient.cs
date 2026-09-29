using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Messenger.Tests;

internal sealed class FakeAuthenticationClient(Func<VerifyTokenRequest, VerifyResponse> verifyToken) : Authentication.AuthenticationClient
{
    public List<VerifyTokenRequest> Requests { get; } = [];

    public override AsyncUnaryCall<VerifyResponse> VerifySecurityTokenAsync(
        VerifyTokenRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        var response = verifyToken(request);

        return new AsyncUnaryCall<VerifyResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }
}
