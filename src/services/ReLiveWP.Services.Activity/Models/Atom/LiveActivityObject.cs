using System.Xml.Serialization;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("object", Namespace = Constants.ActivityStreams_Namespace)]
public class LiveActivityObject
{
    [XmlElement(ElementName = "object-type", Namespace = Constants.ActivityStreams_Namespace)]
    public string ObjectType { get; set; } = default!;

    [XmlElement(ElementName = "id", Namespace = Constants.Atom_Namespace)]
    public string Id { get; set; } = default!;

    [XmlElement(ElementName = "title", Namespace = Constants.Atom_Namespace)]
    public Content Title { get; set; } = default!;

    [XmlElement(ElementName = "content", Namespace = Constants.Atom_Namespace)]
    public Content Content { get; set; } = default!;

    [XmlElement(ElementName = "link", Namespace = Constants.Atom_Namespace)]
    public List<Link> Links { get; set; } = [];
}
