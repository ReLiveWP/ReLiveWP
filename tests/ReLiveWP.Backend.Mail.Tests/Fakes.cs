using Grpc.Core;
using ReLiveWP.Backend.Mail.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mail.Tests;

internal sealed class FakeUserClient : User.UserClient
{
    public Func<LookupUsersByEmailRequest, LookupUsersByEmailResponse>? OnLookupUsersByEmail { get; set; }
    public Func<GetUserInfoRequest, GetUserInfoResponse>? OnGetUserInfo { get; set; }

    public LookupUsersByEmailRequest? LastLookup { get; private set; }

    public override AsyncUnaryCall<LookupUsersByEmailResponse> LookupUsersByEmailAsync(
        LookupUsersByEmailRequest request, CallOptions options)
    {
        LastLookup = request;
        return Unary(() => OnLookupUsersByEmail is null
            ? new LookupUsersByEmailResponse()
            : OnLookupUsersByEmail(request));
    }

    public override AsyncUnaryCall<GetUserInfoResponse> GetUserInfoAsync(
        GetUserInfoRequest request, CallOptions options)
    {
        if (OnGetUserInfo is null)
            throw new InvalidOperationException($"{nameof(OnGetUserInfo)} not configured");
        return Unary(() => OnGetUserInfo(request));
    }

    private static AsyncUnaryCall<T> Unary<T>(Func<T> body)
    {
        Task<T> task;
        try
        {
            task = Task.FromResult(body());
        }
        catch (Exception ex)
        {
            task = Task.FromException<T>(ex);
        }

        return new AsyncUnaryCall<T>(
            task,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}

internal sealed class FakeMailQueue : IMailQueue
{
    public List<(MailEnvelope Envelope, byte[] Message)> Enqueued { get; } = [];

    public Task EnqueueAsync(MailEnvelope envelope, byte[] message, CancellationToken ct)
    {
        Enqueued.Add((envelope, message));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QueuedMail>> DequeueAsync(int count, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<QueuedMail>>([]);

    public Task CompleteAsync(QueuedMail item, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeSentItemsWriter : ISentItemsWriter
{
    public List<(string UserId, string SubmissionId, byte[] Message)> Written { get; } = [];

    public Func<Task>? OnWrite { get; set; }

    public async Task WriteAsync(
        string userId, string submissionId, string fromAddress, byte[] message, CancellationToken ct)
    {
        if (OnWrite is not null)
            await OnWrite();
        Written.Add((userId, submissionId, message));
    }
}

internal sealed class FakeDeliveryAgent(MailRoute route) : IMailDeliveryAgent
{
    public MailRoute Route { get; } = route;

    public List<MailEnvelope> Delivered { get; } = [];

    public Task DeliverAsync(MailEnvelope envelope, byte[] message, CancellationToken ct)
    {
        Delivered.Add(envelope);
        return Task.CompletedTask;
    }
}

// minimal ServerCallContext for calling RPCs without hosting a gRPC server; only CancellationToken
// is wired up
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
