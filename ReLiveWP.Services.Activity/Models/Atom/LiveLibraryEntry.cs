using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("media", Constants.MediaRss_Namespace)]
[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LiveLibraryEntry : Entry
{
    // "Library" on write; "Folder" / "Photo" / "Video" on read.
    [XmlElement(ElementName = "type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = default!;

    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;

    [XmlElement(ElementName = "category", Namespace = Constants.Live_Namespace)]
    public string Category { get; set; } = default!;

    [XmlElement(ElementName = "canonicalName", Namespace = Constants.Live_Namespace)]
    public string CanonicalName { get; set; } = default!;

    [XmlElement(ElementName = "sharinglevel", Namespace = Constants.Live_Namespace)]
    public string SharingLevel { get; set; } = default!;

    [XmlElement(ElementName = "emailKeyword", Namespace = Constants.Live_Namespace)]
    public string EmailKeyword { get; set; } = default!;

    [XmlElement(ElementName = "thumbnail", Namespace = Constants.MediaRss_Namespace)]
    public List<LiveMediaThumbnail> Thumbnails { get; set; } = [];
}
