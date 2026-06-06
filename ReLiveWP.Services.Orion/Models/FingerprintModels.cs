using System;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Orion.Models;


[XmlType(Namespace = Ns)]
public class DeviceProfile
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute] public string DeviceType { get; set; }
    [XmlAttribute] public string ClientGuid { get; set; }
    [XmlAttribute] public string OSVersion { get; set; }
    [XmlAttribute] public string LFVersion { get; set; }
    [XmlAttribute] public string ExtendedDeviceInfo { get; set; }
    [XmlAttribute] public string Platform { get; set; }
    [XmlAttribute] public string DeviceId { get; set; }
}

[XmlType(Namespace = Ns)]
public class MarketIdentifier
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute] public string RegionIdentifier { get; set; }
    [XmlAttribute] public string OperatorIdentifier { get; set; }
    [XmlAttribute] public string NetworkType { get; set; }
}

[XmlType(Namespace = Ns)]
public class RequestHeader
{
    const string Ns = "http://inference.location.live.com";

    public string ApplicationId { get; set; }
    public DeviceProfile DeviceProfile { get; set; }
    public string Authorization { get; set; }
    public string TrackingId { get; set; }
    public string Timestamp { get; set; }
    public MarketIdentifier MarketIdentifier { get; set; }
}

// ── Beacon detection types ───────────────────────────────────────────────

[XmlType(TypeName = "Mrl7", Namespace = Ns)]
public class Mrl7
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("mcc")] public int Mcc { get; set; }
    [XmlAttribute("mnc")] public int Mnc { get; set; }
    [XmlAttribute("cid")] public long Cid { get; set; }
    [XmlAttribute("lac")] public int Lac { get; set; }
    [XmlAttribute("uarfcn")] public int Uarfcn { get; set; }
    [XmlAttribute("psc")] public int Psc { get; set; }
    [XmlAttribute("rscp")] public int Rscp { get; set; }
    [XmlAttribute("ecno")] public int Ecno { get; set; }
    [XmlAttribute("pathloss")] public int Pathloss { get; set; }
}

[XmlType(TypeName = "Nmr7", Namespace = Ns)]
public class Nmr7
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("mcc")] public int Mcc { get; set; }
    [XmlAttribute("mnc")] public int Mnc { get; set; }
    [XmlAttribute("cid")] public long Cid { get; set; }
    [XmlAttribute("lac")] public int Lac { get; set; }
    [XmlAttribute("arfcn")] public int Arfcn { get; set; }
    [XmlAttribute("baseid")] public int BaseId { get; set; }
    [XmlAttribute("rx")] public int Rx { get; set; }
}

[XmlType(TypeName = "Umts7", Namespace = Ns)]
public class Umts7
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("mcc")] public int Mcc { get; set; }
    [XmlAttribute("mnc")] public int Mnc { get; set; }
    [XmlAttribute("cid")] public long Cid { get; set; }
    [XmlAttribute("lac")] public int Lac { get; set; }
    [XmlAttribute("uarfcn")] public int Uarfcn { get; set; }
    [XmlAttribute("psc")] public int Psc { get; set; }
    [XmlAttribute("frul")] public int Frul { get; set; }
    [XmlAttribute("frdl")] public int Frdl { get; set; }
    [XmlAttribute("frnt")] public int Frnt { get; set; }
    [XmlAttribute("rscp")] public int Rscp { get; set; }
    [XmlAttribute("ecno")] public int Ecno { get; set; }
    [XmlAttribute("pathloss")] public int Pathloss { get; set; }

    [XmlElement("Nmr7")] public List<Nmr7> Nmr7 { get; set; } = new();
    [XmlElement("Mrl7")] public List<Mrl7> Mrl7 { get; set; } = new();
}

[XmlType(TypeName = "Wifi7", Namespace = Ns)]
public class Wifi7
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("BssId")] public string BssId { get; set; }
    [XmlAttribute("rssi")] public int Rssi { get; set; }
}

[XmlType(Namespace = Ns)]
public class Detections
{
    const string Ns = "http://inference.location.live.com";

    [XmlElement("Umts7")] public List<Umts7> Umts7 { get; set; } = new();
    [XmlElement("Wifi7")] public List<Wifi7> Wifi7 { get; set; } = new();
}

[XmlType(Namespace = Ns)]
public class BeaconFingerprint
{
    const string Ns = "http://inference.location.live.com";

    public Detections Detections { get; set; }
}

[XmlRoot("GetLocationUsingFingerprint", Namespace = "http://inference.location.live.com")]
public class GetLocationUsingFingerprintRequest
{
    public RequestHeader RequestHeader { get; set; }
    public BeaconFingerprint BeaconFingerprint { get; set; }
}

[XmlType(Namespace = Ns)]
public class ResolverStatus
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("Status")] public string Status { get; set; }
    [XmlAttribute("Source")] public string Source { get; set; }
}

[XmlType(Namespace = Ns)]
public class ResolvedPosition
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("Latitude")] public double Latitude { get; set; }
    [XmlAttribute("Longitude")] public double Longitude { get; set; }
    [XmlAttribute("Altitude")] public double Altitude { get; set; }
}

[XmlType(Namespace = Ns)]
public class LocationResult
{
    const string Ns = "http://inference.location.live.com";

    public ResolverStatus ResolverStatus { get; set; }
    public ResolvedPosition ResolvedPosition { get; set; }
    public int RadialUncertainty { get; set; }
    public string TileResult { get; set; }
    public string TrackingId { get; set; }
}

[XmlType(Namespace = Ns)]
public class ExtendedV21Result
{
    const string Ns = "http://inference.location.live.com";

    [XmlAttribute("CrowdSourcingLevel")] public string CrowdSourcingLevel { get; set; }
    [XmlAttribute("ServerUtcTime")] public string ServerUtcTime { get; set; }
}

[XmlType(Namespace = Ns)]
public class GetLocationUsingFingerprintResult
{
    const string Ns = "http://inference.location.live.com";

    public string ResponseStatus { get; set; }
    public LocationResult LocationResult { get; set; }
    public ExtendedV21Result ExtendedV21Result { get; set; }
}

[XmlRoot("GetLocationUsingFingerprintResponse", Namespace = "http://inference.location.live.com")]
public class GetLocationUsingFingerprintResponse
{
    public GetLocationUsingFingerprintResult GetLocationUsingFingerprintResult { get; set; }
}
