using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class ProfileResponse
{
    [XmlElement("ProfileId", Namespace = ProfileConstants.Ns)]
    public ProfileId? ProfileId { get; set; }

    [XmlElement("View", Namespace = ProfileConstants.Ns)]
    public ProfileView View { get; set; } = new();
}
