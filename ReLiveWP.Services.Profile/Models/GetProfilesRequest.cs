using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class GetProfilesRequest
{
    [XmlElement("ViewName", Namespace = ProfileConstants.Ns)]
    public string? ViewName { get; set; }

    [XmlArray("Ids", Namespace = ProfileConstants.Ns)]
    [XmlArrayItem("ProfileId", Namespace = ProfileConstants.Ns)]
    public List<ProfileId> Ids { get; set; } = [];
}
