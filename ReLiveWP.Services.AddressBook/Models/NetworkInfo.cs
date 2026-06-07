using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

public class NetworkInfo
{
    [XmlArray("Annotations", IsNullable = true)]
    [XmlArrayItem("Annotation")]
    public List<Annotation>? Annotations { get; set; }

    // IsSNSupported(DomainId) must be true and DomainId != 7 (7 = Windows Live itself). Known-valid: 7, 8, 22.
    [XmlElement("DomainId")]
    public int DomainId { get; set; }

    [XmlElement("SourceId")]
    public string? SourceId { get; set; }

    // DomainTag must be non-empty and not "$null" or the client rejects the network.
    [XmlElement("DomainTag")]
    public string? DomainTag { get; set; }

    [XmlElement("UserTileURL")]
    public string? UserTileURL { get; set; }

    [XmlElement("ProfileURL")]
    public string? ProfileURL { get; set; }

    // DisplayName must be non-empty and not "$null" or the client rejects the network.
    [XmlElement("DisplayName")]
    public string? DisplayName { get; set; }

    [XmlElement("RelationshipType")]
    public int RelationshipType { get; set; }

    [XmlElement("RelationshipState")]
    public int RelationshipState { get; set; }

    [XmlElement("RelationshipStateDate")]
    public DateTime RelationshipStateDate { get; set; }

    [XmlElement("RelationshipRole")]
    public long RelationshipRole { get; set; }

    [XmlElement("ExtendedData")]
    public string? ExtendedData { get; set; }

    [XmlElement("NDRCount")]
    public int NDRCount { get; set; }

    [XmlElement("CreateDate")]
    public DateTime CreateDate { get; set; }

    [XmlElement("LastChanged")]
    public DateTime LastChanged { get; set; }
}
