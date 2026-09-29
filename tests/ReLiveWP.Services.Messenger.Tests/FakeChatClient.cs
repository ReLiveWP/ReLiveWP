using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Services.Messenger.Tests;

internal sealed class FakeChatClient : Chat.ChatClient
{
    public List<RegisterEndpointRequest> Registered { get; } = [];
    public List<SetPresenceRequest> PresenceSet { get; } = [];
    public List<ReportActivityRequest> ActivityReported { get; } = [];
    public List<EndEndpointRequest> Ended { get; } = [];
    public List<SendMessageRequest> Sent { get; } = [];
    public List<string> Calls { get; } = [];

    public EndpointState EndpointState { get; set; } = EndpointState.Known;
    public SendOutcome SendOutcome { get; set; } = SendOutcome.Delivered;
    public StatusCode? FailWith { get; set; }

    private EndpointStateResponse CurrentState() => new() { State = EndpointState };

    public override AsyncUnaryCall<Empty> RegisterEndpointAsync(
        RegisterEndpointRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        Registered.Add(request);
        Calls.Add("RegisterEndpoint");
        return Reply(new Empty());
    }

    public override AsyncUnaryCall<SendMessageResponse> SendMessageAsync(
        SendMessageRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        Sent.Add(request);
        Calls.Add("SendMessage");
        return Reply(new SendMessageResponse { Outcome = SendOutcome });
    }

    public override AsyncUnaryCall<EndpointStateResponse> SetPresenceAsync(
        SetPresenceRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        PresenceSet.Add(request);
        return Reply(CurrentState());
    }

    public override AsyncUnaryCall<EndpointStateResponse> ReportActivityAsync(
        ReportActivityRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        ActivityReported.Add(request);
        return Reply(CurrentState());
    }

    public override AsyncUnaryCall<Empty> EndEndpointAsync(
        EndEndpointRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        Ended.Add(request);
        return Reply(new Empty());
    }

    private void ThrowIfFailing()
    {
        if (FailWith is { } code)
            throw new RpcException(new Status(code, "fake chat failure"));
    }

    private static AsyncUnaryCall<T> Reply<T>(T response) =>
        new(Task.FromResult(response), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], () => { });
}
