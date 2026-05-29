using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Data.Entities;

/// <summary>
/// Typed Windows Live annotation row for a <see cref="ContactItem"/>.
/// One row per contact, one column per annotation field — replaces the old generic
/// EAV <c>ItemAnnotation</c> table.
/// </summary>
public class ContactAnnotation : ILiveAnnotatable
{
    // 1:1 with ContactItem — ContactItemId is both PK and FK, no surrogate key needed.
    public string ContactItemId { get; set; } = null!;
    public ContactItem ContactItem { get; set; } = null!;

    // ── WP7-requested annotation fields ──────────────────────────────────────

    /// <summary>CID — 64-bit Windows Live People Store contact identifier (PUID).</summary>
    public long? Cid { get; set; }

    /// <summary>OID — stable string object ID for the contact record.</summary>
    public string? ObjectId { get; set; }

    /// <summary>WLID — Windows Live ID (email address) for this contact.</summary>
    public string? WLId { get; set; }

    /// <summary>IMMRI — IM machine-readable identifier, e.g. "1:foo@hotmail.com".</summary>
    public string? ImMri { get; set; }

    /// <summary>Type — contact type string, e.g. "Regular", "LiveProfile".</summary>
    public string? ContactType { get; set; }

    /// <summary>UserTileUrl — profile picture URL (medium size).</summary>
    public string? UserTileUrl { get; set; }

    /// <summary>UserTileHash — hash of the profile picture for change detection.</summary>
    public string? UserTileHash { get; set; }

    // ── Extended fields ───────────────────────────────────────────────────────

    /// <summary>TrustLevel — social trust level (0 = none, 1 = friend, …).</summary>
    public int? TrustLevel { get; set; }

    /// <summary>FavoriteOrder — position in the favourites list; 0 = not a favourite.</summary>
    public int? FavoriteOrder { get; set; }

    // ── ILiveAnnotatable ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public Annotations? BuildAnnotations(IReadOnlySet<string> requested)
    {
        var items = new List<Annotation>();

        void Add(string name, string? value)
        {
            if (requested.Contains(name) && value is not null)
                items.Add(new Annotation { Name = name, Value = value });
        }

        Add("CID",          Cid?.ToString());
        Add("OID",          ObjectId);
        Add("WLID",         WLId);
        Add("IMMRI",        ImMri);
        Add("Type",         ContactType);
        Add("UserTileUrl",  UserTileUrl);
        Add("UserTileHash", UserTileHash);
        Add("TrustLevel",   TrustLevel?.ToString());
        Add("FavoriteOrder",FavoriteOrder?.ToString());

        return items.Count > 0 ? new Annotations { Items = items } : null;
    }
}
