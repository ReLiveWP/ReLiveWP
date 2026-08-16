namespace ReLiveWP.Services.Exchange.Services;

public class EasSyncOptions
{
    public const string SectionName = "Eas:Sync";

    /// <summary>
    /// MS-ASCMD 2.2.3.179 rule 1: when the client's SyncKey=0 request carried no Supported element
    /// at all, every ghostable element is non-ghosted, so omitting one from a later Change deletes
    /// it. That is spec-correct and irreversibly destructive, so it is opt-in. Off, an absent
    /// Supported behaves like an empty one and everything omitted is preserved.
    /// <para>
    /// The client's actual declaration is cached either way, so flipping this never requires a
    /// device to re-prime. It does mean the flag is inert for an existing sync relationship until
    /// that collection next primes at SyncKey 0, because every row written before this option
    /// existed records "preserve everything".
    /// </para>
    /// <para>
    /// Only Contact and Calendar are affected. Email, Task and Note changes are always overlays,
    /// which is consistent with the Supported schema admitting only the calendar, contacts and
    /// contacts2 ghosting groups.
    /// </para>
    /// </summary>
    public bool AbsentSupportedClearsOmitted { get; set; }
}
