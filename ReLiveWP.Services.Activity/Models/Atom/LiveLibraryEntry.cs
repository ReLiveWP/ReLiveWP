using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LiveLibraryEntry : Entry
{
    // "Library" on write; "Folder" / "Photo" / "Video" on read.
    [XmlElement(ElementName = "type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = default!;

    // WLPGetAlbumInfoParser (WLProv.dll) reads <live:resourceId> (tag 0x3e) into the WLAlbum and
    // the album is only valid/addressable once it's set; without it a GET wmphotos feed parses to
    // an empty/invalid library and the background auto-upload agent loops the GET (mirrors the
    // LivePhotoEntry.ResourceId requirement on the upload response). Left unset on the PUT request.
    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;

    [XmlElement(ElementName = "category", Namespace = Constants.Live_Namespace)]
    public string Category { get; set; } = default!;

    [XmlElement(ElementName = "sharinglevel", Namespace = Constants.Live_Namespace)]
    public string SharingLevel { get; set; } = default!;

    [XmlElement(ElementName = "emailKeyword", Namespace = Constants.Live_Namespace)]
    public string EmailKeyword { get; set; } = default!;
}
