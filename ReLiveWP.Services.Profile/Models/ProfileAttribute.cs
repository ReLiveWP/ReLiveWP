using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class ProfileAttribute
{
    [XmlElement("N", Namespace = ProfileConstants.Ns)] public string? Name { get; set; }
    [XmlElement("V", Namespace = ProfileConstants.Ns)] public ProfileValue? Value { get; set; }

    public ProfileAttribute() { }
    public ProfileAttribute(string name, ProfileValue value) { Name = name; Value = value; }
}
