using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("media", Constants.MediaRss_Namespace)]
[NamespacePrefix("a", Constants.Atom_Namespace)]
[XmlRoot(ElementName = "feed", Namespace = Constants.Atom_Namespace)]
public class LiveAlbumFeed : Root
{
    [XmlElement(ElementName = "type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = "Folder";

    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;

    [XmlElement(ElementName = "category", Namespace = Constants.Live_Namespace)]
    public string? Category { get; set; }

    [XmlElement(ElementName = "sharinglevel", Namespace = Constants.Live_Namespace)]
    public string? SharingLevel { get; set; }

    [XmlElement(ElementName = "emailKeyword", Namespace = Constants.Live_Namespace)]
    public string? EmailKeyword { get; set; }

    [XmlElement(ElementName = "canonicalName", Namespace = Constants.Live_Namespace)]
    public string? CanonicalName { get; set; }

    [XmlElement(ElementName = "itemCount", Namespace = Constants.Live_Namespace)]
    public int ItemCount { get; set; }

    // must stay above Entries. the parser latches on the first entry and hard-errors on any
    // album-level element after it, and XmlSerializer writes members in declaration order.
    [XmlElement(ElementName = "thumbnail", Namespace = Constants.MediaRss_Namespace)]
    public List<LiveMediaThumbnail> Thumbnails { get; set; } = [];

    [XmlElement(ElementName = "entry")]
    public List<LiveAlbumItemEntry> Entries { get; set; } = [];
}
