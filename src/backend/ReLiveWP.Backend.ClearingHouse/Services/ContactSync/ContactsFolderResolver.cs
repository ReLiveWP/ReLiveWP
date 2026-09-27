using System.Collections.Concurrent;
using Grpc.Core;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactsFolderResolver(IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<string, string> _byUser = new();

    public async ValueTask<string> ResolveAsync(string userId, CancellationToken ct = default)
    {
        if (_byUser.TryGetValue(userId, out var cached)) return cached;

        using var scope = scopeFactory.CreateScope();
        var mailbox = scope.ServiceProvider.GetRequiredService<MailboxStore.MailboxStoreClient>();

        using var call = mailbox.ListFolders(new ListFoldersRequest { UserId = userId }, cancellationToken: ct);

        string? id = null;

        await foreach (var folder in call.ResponseStream.ReadAllAsync(ct))
            if (id is null && folder.Type == FolderType.ContactsDefault)
                id = folder.Id;

        if (id is null)
            throw new MirrorException($"User {userId} has no default Contacts folder");

        return _byUser[userId] = id;
    }
}
