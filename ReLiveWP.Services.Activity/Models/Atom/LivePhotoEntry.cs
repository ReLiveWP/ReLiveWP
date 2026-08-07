using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LivePhotoEntry : Entry
{
    // "Photo" / "Video" on read; the inbound upload entry uses lowercase "photo".
    [XmlElement(ElementName = "type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = "Photo";

    [XmlElement(ElementName = "category", Namespace = Constants.Live_Namespace)]
    public string Category { get; set; } = "photos";
    
    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;
}
