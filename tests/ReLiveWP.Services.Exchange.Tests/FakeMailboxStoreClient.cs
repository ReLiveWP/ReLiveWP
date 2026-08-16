using Grpc.Core;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// overriding the (request, CallOptions) overload is enough - the friendlier overloads
// production code uses all forward to it
public class FakeMailboxStoreClient : MailboxStore.MailboxStoreClient
{
    public Func<GetItemRequest, Item>? OnGetItem { get; set; }
    public Func<EmptyFolderRequest, EmptyFolderResult>? OnEmptyFolder { get; set; }
    public Func<GetAttachmentContentRequest, AttachmentContent>? OnGetAttachmentContent { get; set; }
    public Func<MoveConversationRequest, MoveConversationResult>? OnMoveConversation { get; set; }
    public Func<UpdateItemRequest, MutationResult>? OnUpdateItem { get; set; }
    public Func<GetFolderRequest, Folder>? OnGetFolder { get; set; }
    public Func<GetSyncStateRequest, SyncState>? OnGetSyncState { get; set; }
    public Func<UpsertSyncStateRequest, SyncState>? OnUpsertSyncState { get; set; }
    public Func<GetItemEventsRequest, IEnumerable<ItemEvent>>? OnGetItemEvents { get; set; }

    public override AsyncServerStreamingCall<ItemEvent> GetItemEvents(GetItemEventsRequest request, CallOptions options) =>
        ServerStreaming(OnGetItemEvents?.Invoke(request) ?? []);

    public override AsyncUnaryCall<SyncState> GetSyncStateAsync(GetSyncStateRequest request, CallOptions options)
    {
        if (OnGetSyncState is null)
            throw new InvalidOperationException($"{nameof(OnGetSyncState)} not configured");
        return Unary(() => OnGetSyncState(request));
    }

    public override AsyncUnaryCall<SyncState> UpsertSyncStateAsync(UpsertSyncStateRequest request, CallOptions options)
    {
        if (OnUpsertSyncState is null)
            throw new InvalidOperationException($"{nameof(OnUpsertSyncState)} not configured");
        return Unary(() => OnUpsertSyncState(request));
    }

    public override AsyncUnaryCall<MutationResult> UpdateItemAsync(UpdateItemRequest request, CallOptions options)
    {
        if (OnUpdateItem is null)
            throw new InvalidOperationException($"{nameof(OnUpdateItem)} not configured");
        return Unary(() => OnUpdateItem(request));
    }

    public override AsyncUnaryCall<Folder> GetFolderAsync(GetFolderRequest request, CallOptions options)
    {
        if (OnGetFolder is null)
            throw new InvalidOperationException($"{nameof(OnGetFolder)} not configured");
        return Unary(() => OnGetFolder(request));
    }

    public override AsyncUnaryCall<Item> GetItemAsync(GetItemRequest request, CallOptions options)
    {
        if (OnGetItem is null)
            throw new InvalidOperationException($"{nameof(OnGetItem)} not configured");
        return Unary(() => OnGetItem(request));
    }

    public override AsyncUnaryCall<EmptyFolderResult> EmptyFolderAsync(EmptyFolderRequest request, CallOptions options)
    {
        if (OnEmptyFolder is null)
            throw new InvalidOperationException($"{nameof(OnEmptyFolder)} not configured");
        return Unary(() => OnEmptyFolder(request));
    }

    public override AsyncUnaryCall<AttachmentContent> GetAttachmentContentAsync(GetAttachmentContentRequest request, CallOptions options)
    {
        if (OnGetAttachmentContent is null)
            throw new InvalidOperationException($"{nameof(OnGetAttachmentContent)} not configured");
        return Unary(() => OnGetAttachmentContent(request));
    }

    public override AsyncUnaryCall<MoveConversationResult> MoveConversationAsync(MoveConversationRequest request, CallOptions options)
    {
        if (OnMoveConversation is null)
            throw new InvalidOperationException($"{nameof(OnMoveConversation)} not configured");
        return Unary(() => OnMoveConversation(request));
    }

    public Func<CreateItemRequest, Item>? OnCreateItem { get; set; }
    public Func<GetItemsRequest, IEnumerable<Item>>? OnGetItems { get; set; }

    public override AsyncUnaryCall<Item> CreateItemAsync(CreateItemRequest request, CallOptions options)
    {
        if (OnCreateItem is null)
            throw new InvalidOperationException($"{nameof(OnCreateItem)} not configured");
        return Unary(() => OnCreateItem(request));
    }

    public override AsyncServerStreamingCall<Item> GetItems(GetItemsRequest request, CallOptions options) =>
        ServerStreaming(OnGetItems?.Invoke(request) ?? []);

    private static AsyncServerStreamingCall<T> ServerStreaming<T>(IEnumerable<T> items) =>
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
