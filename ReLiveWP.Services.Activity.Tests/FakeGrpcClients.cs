using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Tests;

internal static class TestCache
{
    public static IMemoryCache New() => new MemoryCache(new MemoryCacheOptions());
}

// overriding the (request, CallOptions) overload is enough, the friendlier overloads all forward to it
public class FakeMailboxStoreClient : MailboxStore.MailboxStoreClient
{
    public Func<ResolveFeedSubjectsRequest, ResolveFeedSubjectsResponse>? OnResolveFeedSubjects { get; set; }

    public override AsyncUnaryCall<ResolveFeedSubjectsResponse> ResolveFeedSubjectsAsync(
        ResolveFeedSubjectsRequest request, CallOptions options)
    {
        if (OnResolveFeedSubjects is null)
            throw new InvalidOperationException($"{nameof(OnResolveFeedSubjects)} not configured");

        return FakeCall.Unary(() => OnResolveFeedSubjects(request));
    }

    public Func<ResolveAuthorsToContactsRequest, ResolveAuthorsToContactsResponse>? OnResolveAuthorsToContacts { get; set; }

    public override AsyncUnaryCall<ResolveAuthorsToContactsResponse> ResolveAuthorsToContactsAsync(
        ResolveAuthorsToContactsRequest request, CallOptions options) =>
        FakeCall.Unary(() => OnResolveAuthorsToContacts?.Invoke(request) ?? new ResolveAuthorsToContactsResponse());
}

public class FakeConnectedServicesClient : ConnectedServices.ConnectedServicesClient
{
    public Func<SharedConnectionsRequest, SharedConnectionsResponse>? OnGetSharedConnections { get; set; }

    public int GetSharedConnectionsCalls { get; private set; }

    public override AsyncUnaryCall<SharedConnectionsResponse> GetSharedConnectionsAsync(
        SharedConnectionsRequest request, CallOptions options)
    {
        GetSharedConnectionsCalls++;

        if (OnGetSharedConnections is null)
            throw new InvalidOperationException($"{nameof(OnGetSharedConnections)} not configured");

        return FakeCall.Unary(() => OnGetSharedConnections(request));
    }

    public Func<ConnectionsRequest, IEnumerable<Connection>>? OnGetConnections { get; set; }

    public override AsyncServerStreamingCall<Connection> GetConnections(ConnectionsRequest request, CallOptions options) =>
        FakeCall.ServerStreaming(OnGetConnections?.Invoke(request) ?? []);
}

internal static class FakeCall
{
    public static AsyncServerStreamingCall<T> ServerStreaming<T>(IEnumerable<T> items) =>
        new(new ListStreamReader<T>(items),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    private sealed class ListStreamReader<T>(IEnumerable<T> items) : IAsyncStreamReader<T>
    {
        private readonly IEnumerator<T> enumerator = items.GetEnumerator();

        public T Current => enumerator.Current;

        public Task<bool> MoveNext(CancellationToken cancellationToken) =>
            Task.FromResult(enumerator.MoveNext());
    }

    public static AsyncUnaryCall<T> Unary<T>(Func<T> body)
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
