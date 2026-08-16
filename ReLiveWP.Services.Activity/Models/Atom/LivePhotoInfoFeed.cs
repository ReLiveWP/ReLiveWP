using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("media", Constants.MediaRss_Namespace)]
[NamespacePrefix("a", Constants.Atom_Namespace)]
[XmlRoot(ElementName = "feed", Namespace = Constants.Atom_Namespace)]
public class LivePhotoInfoFeed : Root
{
    [XmlElement(ElementName = "entry")]
    public List<LiveAlbumItemEntry> Entries { get; set; } = [];
}
