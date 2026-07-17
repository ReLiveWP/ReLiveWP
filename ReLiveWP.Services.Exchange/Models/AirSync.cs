using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

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

    [XmlElement("EstimatedDataSize", Namespace = Constants.AirSyncBase)]
    public int? EstimatedDataSize { get; set; }

    [XmlElement("Truncated", Namespace = Constants.AirSyncBase)]
    public int? Truncated { get; set; }

    [XmlElement("Data", Namespace = Constants.AirSyncBase)]
    public string? Data { get; set; }

    [XmlElement("Preview", Namespace = Constants.AirSyncBase)]
    public string? Preview { get; set; }
}

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
