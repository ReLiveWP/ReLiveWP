namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public enum DbChangeEventType { Add, Update, Delete }

public interface IDbChangeEvent
{
    long Id { get; }

    /// <summary>
    /// Commit-ordered cursor for the change log, defaulted by Postgres to
    /// <c>pg_current_xact_id() + CommitIdOffset</c>.
    /// </summary>
    long CommitId { get; }

    string ServerId { get; }
    DbChangeEventType EventType { get; }
}

public static class ChangeEventCursor
{
    /// <summary>Keeps generated commit ids clear of the backfilled row ids they replaced.</summary>
    public const long CommitIdOffset = 1_000_000_000L;

    public const string CommitIdDefaultSql = "(pg_current_xact_id()::text::bigint + 1000000000)";

    /// <summary>
    /// Highest commit id that is safe to read: everything below the oldest in-flight transaction is
    /// fully resolved, so no lower commit id can still appear.
    /// </summary>
    public const string SafeHorizonSql =
        "(pg_snapshot_xmin(pg_current_snapshot())::text::bigint + 1000000000)";
}
