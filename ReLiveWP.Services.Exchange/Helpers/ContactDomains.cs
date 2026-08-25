namespace ReLiveWP.Services.Exchange.Helpers;

internal enum MriGeneration
{
    ObjectIdBasedXmpp,
    EmailBasedXmpp,
    ObjectIdBasedSkype,
}

internal record ContactDomain(string SourceId, int DomainId, bool IsImEnabled, MriGeneration MriGeneration);

internal static class ContactDomains
{
    private static readonly ContactDomain[] All =
    [
        new("WL",    1,   false, MriGeneration.ObjectIdBasedXmpp),
        new("XBL",   2,   true,  MriGeneration.ObjectIdBasedXmpp),
        new("FB",    7,   true,  MriGeneration.ObjectIdBasedXmpp),
        new("LI",    8,   false, MriGeneration.ObjectIdBasedXmpp),
        new("MYSP",  9,   false, MriGeneration.ObjectIdBasedXmpp),
        new("EXCH",  17,  false, MriGeneration.ObjectIdBasedXmpp),
        new("ABCH",  18,  false, MriGeneration.ObjectIdBasedXmpp),
        new("FLKR",  19,  false, MriGeneration.ObjectIdBasedXmpp),
        new("GOOG",  20,  true,  MriGeneration.EmailBasedXmpp),
        new("YHOO",  21,  true,  MriGeneration.ObjectIdBasedXmpp),
        new("TWITR", 22,  false, MriGeneration.ObjectIdBasedXmpp),
        new("WLP",   127, false, MriGeneration.ObjectIdBasedXmpp),
        new("SINWE", 128, false, MriGeneration.ObjectIdBasedXmpp),
        new("SKYPE", 129, true,  MriGeneration.ObjectIdBasedSkype),
        new("RENN",  132, false, MriGeneration.ObjectIdBasedXmpp),
        new("SCD",   151, true,  MriGeneration.ObjectIdBasedSkype),
    ];

    private static readonly Dictionary<string, ContactDomain> BySourceId =
        All.ToDictionary(d => d.SourceId, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<int, ContactDomain> ByDomainId =
        All.ToDictionary(d => d.DomainId);

    public const string WindowsLive = "WL";
    public const string Abch = "ABCH";

    public static ContactDomain? BySource(string? sourceId)
        => sourceId is not null && BySourceId.TryGetValue(sourceId, out var d) ? d : null;

    public static ContactDomain? ByDomain(int domainId)
        => ByDomainId.TryGetValue(domainId, out var d) ? d : null;
}
