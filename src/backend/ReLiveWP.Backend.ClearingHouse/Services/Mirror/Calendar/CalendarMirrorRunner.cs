using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class CalendarMirrorRunner(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    MirrorDriverRegistry drivers,
    CalendarFolderResolver folders,
    ILogger<CalendarMirrorRunner> logger)
    : MirrorRunner(mailbox, db, drivers, logger)
{
    public override MirrorKind Kind => MirrorKind.Calendar;

    protected override ValueTask<string> ResolveFolderAsync(
        DbSyncSource source, SyncConnection connection, CancellationToken ct) =>
        folders.ResolveAsync(source, ct);
}
