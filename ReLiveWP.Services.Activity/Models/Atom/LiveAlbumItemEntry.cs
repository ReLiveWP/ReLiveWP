using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("media", Constants.MediaRss_Namespace)]
[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LiveAlbumItemEntry : Entry
{
    [XmlElement(ElementName = "type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = default!;

    [XmlElement(ElementName = "resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;

    [XmlElement(ElementName = "commentCount", Namespace = Constants.Live_Namespace)]
    public int CommentCount { get; set; }

    [XmlElement(ElementName = "commentsEnabled", Namespace = Constants.Live_Namespace)]
    public bool CommentsEnabled { get; set; }

    [XmlElement(ElementName = "thumbnail", Namespace = Constants.MediaRss_Namespace)]
    public List<LiveMediaThumbnail> Thumbnails { get; set; } = [];

    [XmlElement(ElementName = "content", Namespace = Constants.MediaRss_Namespace)]
    public LiveMediaContent? MediaContent { get; set; }
}
