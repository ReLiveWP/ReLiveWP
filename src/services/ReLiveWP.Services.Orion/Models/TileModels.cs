using System.Xml.Serialization;

namespace ReLiveWP.Services.Orion.Models;

[XmlType(Namespace = Ns)]
public class TileParameters
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute] public int TileSizeInBytes { get; set; }
    [XmlAttribute] public string BlobType { get; set; }
    [XmlAttribute] public string DeltaType { get; set; }
    [XmlAttribute] public string IncludeTileData { get; set; }
    [XmlAttribute] public string BeaconGroupMask { get; set; }
}

[XmlType(Namespace = Ns)]
public class Position
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("Latitude")] public double Latitude { get; set; }
    [XmlAttribute("Longitude")] public double Longitude { get; set; }
    [XmlAttribute("Altitude")] public double Altitude { get; set; }
}

[XmlType(Namespace = Ns)]
public class OperatorId
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("mcc")] public int Mcc { get; set; }
    [XmlAttribute("mnc")] public int Mnc { get; set; }
}

[XmlRoot("GetTileUsingPosition", Namespace = "http://inference.location.live.com")]
public class GetTileUsingPositionRequest
{
    public RequestHeader RequestHeader { get; set; }
    public TileParameters TileParameters { get; set; }
    public Position Position { get; set; }
    public OperatorId OperatorId { get; set; }
}

[XmlType(Namespace = Ns)]
public class TileSet
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("count")] public int Count { get; set; }
    [XmlAttribute("DataSuppressed")] public bool DataSuppressed { get; set; }

    // No <Tile> children are here yet, a populated tile carries GSM/CDMA/Wi-Fi beacon groups plus
    // RawData/RawDataSize/Base64StringLength, we're simply returning nothing for now
}

[XmlType(Namespace = Ns)]
public class GetTileUsingPositionResult
{
    const string Ns = "http://inference.location.live.com";

    public string ResponseStatus { get; set; }
    public string TrackingId { get; set; }
    public TileSet TileSet { get; set; }
    public ExtendedV21Result ExtendedV21Result { get; set; }
}

[XmlRoot("GetTileUsingPositionResponse", Namespace = "http://inference.location.live.com")]
public class GetTileUsingPositionResponse
{
    public GetTileUsingPositionResult GetTileUsingPositionResult { get; set; }
}
