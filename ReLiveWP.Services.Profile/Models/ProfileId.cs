using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class ProfileId
{
    [XmlElement("Ns1", Namespace = ProfileConstants.Ns)] public string? Ns1 { get; set; }
    [XmlElement("V1", Namespace = ProfileConstants.Ns)] public ProfileValue? V1 { get; set; }
    [XmlElement("Ns2", Namespace = ProfileConstants.Ns)] public string? Ns2 { get; set; }
    [XmlElement("V2", Namespace = ProfileConstants.Ns)] public ProfileValue? V2 { get; set; }
    [XmlElement("Ns3", Namespace = ProfileConstants.Ns)] public string? Ns3 { get; set; }
    [XmlElement("V3", Namespace = ProfileConstants.Ns)] public ProfileValue? V3 { get; set; }
}
