using System.Xml.Serialization;

namespace ReLiveWP.Services.AppServer.Models;

[XmlRoot("Results")]
public class RichClientConfigResults
{
    [XmlElement("UserID")]
    public UserIdResult? UserId { get; set; }
}

public class UserIdResult
{
    [XmlAttribute("Value")]
    public string Value { get; set; } = string.Empty;
}
