using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

public class LiveEntityUser
{
    [XmlElement("ObjectId", Namespace = Constants.Live_Namespace)]
    public string ObjectId { get; set; } = default!;

    [XmlElement("ScreenName", Namespace = Constants.Live_Namespace)]
    public string? ScreenName { get; set; }
}
