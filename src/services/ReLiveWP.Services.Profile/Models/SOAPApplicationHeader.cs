using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class SOAPApplicationHeader
{
    [XmlElement("ApplicationId")] public string? ApplicationId { get; set; }
    [XmlElement("Scenario")] public string? Scenario { get; set; }
    [XmlElement("TransactionId")] public string? TransactionId { get; set; }
}
