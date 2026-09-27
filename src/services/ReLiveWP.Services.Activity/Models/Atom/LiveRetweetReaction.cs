using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("RetweetReaction", Namespace = Constants.Live_Namespace)]
public class LiveRetweetReaction : LiveReaction
{
    [XmlAttribute(AttributeName = "CanReact")]
    public bool CanReact { get; set; } = true;

    [XmlAttribute(AttributeName = "Count")]
    public int Count { get; set; } = 0;
}