using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[XmlInclude(typeof(LiveComment))]
[XmlInclude(typeof(LiveCommentAuthor))]
[XmlRoot(ElementName = "feed", Namespace = Constants.Atom_Namespace)]
public class LiveCommentsFeed : Feed
{
}

[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LiveComment : Entry
{
    [XmlElement("commentId", Namespace = Constants.Live_Namespace)]
    public string CommentId { get; set; } = default!;
}

[XmlRoot("author", Namespace = Constants.Atom_Namespace)]
public class LiveCommentAuthor : Author
{
    [XmlElement("cid", Namespace = Constants.Live_Namespace)]
    public string Cid { get; set; } = default!;
}
