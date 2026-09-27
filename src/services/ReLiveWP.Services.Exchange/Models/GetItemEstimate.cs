using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string GetItemEstimate = "GetItemEstimate";
}

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
    [XmlElement("SyncKey", Namespace = Constants.AirSync)]
    public string SyncKey { get; set; } = "0";

    [XmlElement("CollectionId", Namespace = Constants.GetItemEstimate)]
    public string CollectionId { get; set; } = string.Empty;

    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    [XmlElement("ConversationMode", Namespace = Constants.AirSync)]
    public int? ConversationMode { get; set; }

    [XmlElement("Options", Namespace = Constants.AirSync)]
    public SyncOptions? Options { get; set; }
}

[XmlRoot("GetItemEstimate", Namespace = Constants.GetItemEstimate)]
public class GetItemEstimateResponse
{
    [XmlElement("Response", Namespace = Constants.GetItemEstimate)]
    public List<GieResponse> Responses { get; set; } = [];
}

public class GieResponse
{
    // 1=Success 2=InvalidCollection 3=NotPrimed 4=InvalidSyncKey
    [XmlElement("Status", Namespace = Constants.GetItemEstimate)]
    public int Status { get; set; }

    [XmlElement("Collection", Namespace = Constants.GetItemEstimate)]
    public GieResponseCollection? Collection { get; set; }
}

public class GieResponseCollection
{
    [XmlElement("CollectionId", Namespace = Constants.GetItemEstimate)]
    public string CollectionId { get; set; } = string.Empty;

    [XmlElement("Class", Namespace = Constants.GetItemEstimate)]
    public string? Class { get; set; }

    [XmlElement("Estimate", Namespace = Constants.GetItemEstimate)]
    public int Estimate { get; set; }
}
