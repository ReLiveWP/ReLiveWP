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

    // WLPUploadParser::_Parse (WLProv.dll @1003da44) requires the upload response <entry> to
    // carry BOTH a non-empty atom <id> and a non-empty <live:resourceId>; either being empty
    // makes WLParserBase::Parse return E_FAIL (0x80004005), which the device treats as an upload
    // failure and retries indefinitely. The device keys subsequent files('<id>') operations off
    // this resourceId.
    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;
}
