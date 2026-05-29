using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string GetItemEstimate = "GetItemEstimate";
}

// ── Request ───────────────────────────────────────────────────────────────────
// The root and most child elements use the GetItemEstimate namespace; SyncKey,
// Class, Options, ConversationMode are in the AirSync namespace (per spec §2.2.1.9).

[XmlRoot("GetItemEstimate", Namespace = Constants.GetItemEstimate)]
public class GetItemEstimateRequest
{
    [XmlElement("Collections", Namespace = Constants.GetItemEstimate)]
    public GieCollections? Collections { get; set; }
}

public class GieCollections
{
    [XmlElement("Collection", Namespace = Constants.GetItemEstimate)]
    public List<GieRequestCollection> Items { get; set; } = [];
}

public class GieRequestCollection
{
    // SyncKey and Class use the AirSync namespace in requests (§2.2.3.181.3)
    [XmlElement("SyncKey", Namespace = Constants.AirSync)]
    public string SyncKey { get; set; } = "0";

    [XmlElement("CollectionId", Namespace = Constants.GetItemEstimate)]
    public string CollectionId { get; set; } = string.Empty;

    // Used with protocol 2.5/12.0/12.1 only
    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    [XmlElement("ConversationMode", Namespace = Constants.AirSync)]
    public int? ConversationMode { get; set; }

    // Options: FilterType, BodyPreference etc. (AirSync namespace)
    [XmlElement("Options", Namespace = Constants.AirSync)]
    public SyncOptions? Options { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────────────
// All response elements are in the GetItemEstimate namespace.
// One <Response> per requested collection; Status inside Response is per-collection.

[XmlRoot("GetItemEstimate", Namespace = Constants.GetItemEstimate)]
public class GetItemEstimateResponse
{
    [XmlElement("Response", Namespace = Constants.GetItemEstimate)]
    public List<GieResponse> Responses { get; set; } = [];
}

public class GieResponse
{
    // Status 1=Success 2=InvalidCollection 3=NotPrimed 4=InvalidSyncKey
    [XmlElement("Status", Namespace = Constants.GetItemEstimate)]
    public int Status { get; set; }

    [XmlElement("Collection", Namespace = Constants.GetItemEstimate)]
    public GieResponseCollection? Collection { get; set; }
}

public class GieResponseCollection
{
    [XmlElement("CollectionId", Namespace = Constants.GetItemEstimate)]
    public string CollectionId { get; set; } = string.Empty;

    // Item class: "Email", "Calendar", "Contacts", "Tasks"
    [XmlElement("Class", Namespace = Constants.GetItemEstimate)]
    public string? Class { get; set; }

    [XmlElement("Estimate", Namespace = Constants.GetItemEstimate)]
    public int Estimate { get; set; }
}
