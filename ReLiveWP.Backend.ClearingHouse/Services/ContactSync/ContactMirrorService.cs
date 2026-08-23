using Google.Protobuf;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactMirrorService(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    MirrorDriverRegistry drivers,
    ContactPhotoService photos,
    ContactsFolderResolver folders,
    IConfiguration configuration,
    ILogger<ContactMirrorService> logger)
    : MirrorRunner(mailbox, db, drivers, logger)
{
    private const int DefaultPhotoConcurrency = 8;

    public override MirrorKind Kind => MirrorKind.Contacts;

    protected override ValueTask<string> ResolveFolderAsync(
        DbSyncSource source, SyncConnection connection, CancellationToken ct) =>
        folders.ResolveAsync(connection.UserId, ct);

    protected override Task PrepareAsync(
        IReadOnlyList<MirrorWrite> writes, SyncConnection connection, CancellationToken ct)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = configuration.GetValue("Mirror:Contacts:PhotoConcurrency", DefaultPhotoConcurrency),
            CancellationToken = ct,
        };

        return Parallel.ForEachAsync(writes, options, async (entry, token) =>
        {
            if (entry.Remote is not RemoteContact contact) return;

            if (await photos.ResolveAsync(contact, connection, token) is { } picture)
                contact.Contact.Picture = ByteString.CopyFrom(picture);
        });
    }
}
