using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class GetProfilesResponse
{
    [XmlElement("ProfileResponse", Namespace = ProfileConstants.Ns)]
    public List<ProfileResponse> Profiles { get; set; } = [];
}
