using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

// Shared DTOs for the AirSyncBase namespace (code page 17).
// Used across all item classes: Email, Calendar, Contacts, Tasks.

// <airsyncbase:Body> — response element containing the body/notes for any item.
// Used in Sync, ItemOperations, and Search responses for all item classes.
public class AirSyncBody
{
    [XmlIgnore]
    public BodyType Type { get; set; } = BodyType.PlainText;

    [XmlElement("Type", Namespace = Constants.AirSyncBase)]
    public int TypeInt
    {
        get => (int)Type;
        set => Type = (BodyType)value;
    }

    // Present in responses; omitted when the body has not been truncated and
    // EstimatedDataSize equals the actual data length.
    [XmlElement("EstimatedDataSize", Namespace = Constants.AirSyncBase)]
    public int? EstimatedDataSize { get; set; }

    // 0|1 (EAS boolean); present only when the body was truncated.
    [XmlElement("Truncated", Namespace = Constants.AirSyncBase)]
    public int? Truncated { get; set; }

    [XmlElement("Data", Namespace = Constants.AirSyncBase)]
    public string? Data { get; set; }

    // Short plain-text preview of the body (≤255 chars); returned when the
    // client requested a preview via BodyPreference/Preview.
    [XmlElement("Preview", Namespace = Constants.AirSyncBase)]
    public string? Preview { get; set; }
}

// <airsyncbase:Location> — rich location element used in Calendar v16.0+.
// Replaces the plain-string calendar:Location element.
public class AirSyncLocation
{
    [XmlElement("DisplayName", Namespace = Constants.AirSyncBase)]
    public string? DisplayName { get; set; }

    [XmlElement("Annotation", Namespace = Constants.AirSyncBase)]
    public string? Annotation { get; set; }

    [XmlElement("Street", Namespace = Constants.AirSyncBase)]
    public string? Street { get; set; }

    [XmlElement("City", Namespace = Constants.AirSyncBase)]
    public string? City { get; set; }

    [XmlElement("State", Namespace = Constants.AirSyncBase)]
    public string? State { get; set; }

    [XmlElement("Country", Namespace = Constants.AirSyncBase)]
    public string? Country { get; set; }

    [XmlElement("PostalCode", Namespace = Constants.AirSyncBase)]
    public string? PostalCode { get; set; }

    [XmlElement("Latitude", Namespace = Constants.AirSyncBase)]
    public double? Latitude { get; set; }

    [XmlElement("Longitude", Namespace = Constants.AirSyncBase)]
    public double? Longitude { get; set; }

    [XmlElement("Accuracy", Namespace = Constants.AirSyncBase)]
    public double? Accuracy { get; set; }

    [XmlElement("Altitude", Namespace = Constants.AirSyncBase)]
    public double? Altitude { get; set; }

    [XmlElement("AltitudeAccuracy", Namespace = Constants.AirSyncBase)]
    public double? AltitudeAccuracy { get; set; }

    [XmlElement("LocationUri", Namespace = Constants.AirSyncBase)]
    public string? LocationUri { get; set; }
}
