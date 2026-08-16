using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

internal enum GhostingMode
{
    GhostAll,
    Declared,
    GhostNone,
}

/// <summary>
/// Decides whether an element omitted from a Sync Change should be preserved or deleted.
/// </summary>
internal sealed class GhostingPolicy
{
    private const string GhostNoneMarker = "!none";

    private static readonly HashSet<string> NeverCleared = new(StringComparer.Ordinal)
    {
        Key(Constants.Contacts, "Picture"),
        Key(Constants.AirSyncBase, "Body"),
        Key(Constants.AirSyncBase, "Data"),
        Key(Constants.Contacts, "Body"),
        Key(Constants.Calendar, "Body"),
    };

    private readonly IReadOnlySet<string>? notGhosted;

    private GhostingPolicy(GhostingMode mode, IReadOnlySet<string>? notGhosted = null)
    {
        Mode = mode;
        this.notGhosted = notGhosted;
    }

    public static readonly GhostingPolicy GhostAll = new(GhostingMode.GhostAll);
    public static readonly GhostingPolicy GhostNone = new(GhostingMode.GhostNone);

    public GhostingMode Mode { get; }

    public bool PreservesEverything => Mode == GhostingMode.GhostAll;

    public bool ShouldClear(string ns, string localName)
    {
        var key = Key(ns, localName);
        if (NeverCleared.Contains(key)) return false;

        return Mode switch
        {
            GhostingMode.GhostAll => false,
            GhostingMode.GhostNone => true,
            _ => notGhosted!.Contains(key),
        };
    }

    public bool IsGhosted(string ns, string localName) => !ShouldClear(ns, localName);

    public GhostingPolicy Effective(bool absentSupportedClearsOmitted) =>
        Mode == GhostingMode.GhostNone && !absentSupportedClearsOmitted ? GhostAll : this;

    public static GhostingPolicy FromSupported(SyncSupported? supported)
    {
        if (supported is null) return GhostNone;
        if (supported.Elements.Count == 0) return GhostAll;

        var names = supported.Elements
            .Select(e => Key(e.NamespaceURI, e.LocalName))
            .ToHashSet(StringComparer.Ordinal);

        return names.Count > 0 ? new GhostingPolicy(GhostingMode.Declared, names) : GhostAll;
    }

    public string Serialize() => Mode switch
    {
        GhostingMode.GhostAll => string.Empty,
        GhostingMode.GhostNone => GhostNoneMarker,
        _ => string.Join(",", notGhosted!),
    };

    public static GhostingPolicy Parse(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return GhostAll;
        if (string.Equals(stored, GhostNoneMarker, StringComparison.Ordinal)) return GhostNone;

        var names = stored.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        return names.Count > 0 ? new GhostingPolicy(GhostingMode.Declared, names) : GhostAll;
    }

    private static string Key(string ns, string localName) => $"{ns}:{localName}";
}
