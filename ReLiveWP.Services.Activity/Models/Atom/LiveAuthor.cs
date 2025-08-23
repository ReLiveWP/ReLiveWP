using System.Xml.Serialization;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("author", Namespace = Constants.Atom_Namespace)]
public class LiveAuthor : Author
{
    [XmlElement("id", Namespace = Constants.Live_Namespace)]
    public string Id { get; set; }

    [XmlElement(ElementName = "link")]
    public List<Link> Links { get; set; } = new List<Link>();
}
