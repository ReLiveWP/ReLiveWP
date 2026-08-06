using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

/// <summary>
/// Decides whether an element omitted from a Sync Change should be preserved or deleted.
/// </summary>
internal sealed class GhostingPolicy(IReadOnlySet<string>? notGhosted)
{
    public static readonly GhostingPolicy PreserveAll = new(null);
    
    public bool IsGhosted(string ns, string localName) =>
        notGhosted is null || !notGhosted.Contains(Key(ns, localName));

    public bool ShouldClear(string ns, string localName) => !IsGhosted(ns, localName);

    public bool IsPreserveAll => notGhosted is null;

    private static string Key(string ns, string localName) => $"{ns}:{localName}";
    

    public static GhostingPolicy FromSupported(SyncSupported? supported)
    {
        if (supported?.Elements is not { Count: > 0 }) return PreserveAll;

        var names = supported.Elements
            .Select(e => Key(e.NamespaceURI, e.LocalName))
            .ToHashSet(StringComparer.Ordinal);

        return names.Count > 0 ? new GhostingPolicy(names) : PreserveAll;
    }


    public string Serialize() => notGhosted is null ? string.Empty : string.Join(",", notGhosted);
    
    public static GhostingPolicy Parse(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return PreserveAll;

        var names = stored.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        return names.Count > 0 ? new GhostingPolicy(names) : PreserveAll;
    }
}
