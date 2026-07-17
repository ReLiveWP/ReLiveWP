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
