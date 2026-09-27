using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class ProfileView
{
    [XmlArray("Attributes", Namespace = ProfileConstants.Ns)]
    [XmlArrayItem("A", Namespace = ProfileConstants.Ns)]
    public List<ProfileAttribute> Attributes { get; set; } = [];
}
