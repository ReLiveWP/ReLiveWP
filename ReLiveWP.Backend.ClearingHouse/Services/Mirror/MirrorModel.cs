using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public enum MirrorKind
{
    Contacts,
    Calendar,
}

public sealed record SyncConnection(string UserId, string ConnectionId, string? ServiceUrl = null);

public sealed record RemoteSource(
    string Id,
    string DisplayName,
    int? Count = null,
    bool IsDefault = false);

// the item carries its own write so the runner never has to know which oneof it is filling in
public interface IRemoteItem
{
    string ExternalId { get; }
    string? Etag { get; }

    void ApplyTo(CreateItemRequest request);
    void ApplyTo(UpdateItemRequest request);
}

public sealed record MirrorBatch(
    IReadOnlyList<IRemoteItem> Items,
    IReadOnlyList<string> DeletedExternalIds,
    string? DeltaToken,
    bool IsFullSync,
    IReadOnlyList<string>? UnreadableExternalIds = null)
{
    public IReadOnlyList<string> Unreadable => UnreadableExternalIds ?? [];
}

public sealed record MirrorRunResult(int Created, int Updated, int Deleted, int Skipped)
{
    public bool DidNothing => Created == 0 && Updated == 0 && Deleted == 0;
}
