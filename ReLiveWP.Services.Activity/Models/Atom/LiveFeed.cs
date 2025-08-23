using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("activity", Constants.ActivityStreams_Namespace)]
[NamespacePrefix("threads", Constants.Threads_Namespace)]
[NamespacePrefix("media", Constants.Media_Namespace)]
[XmlInclude(typeof(LiveAuthor))]
[XmlInclude(typeof(LiveEntry))]
[XmlRoot(ElementName = "feed", Namespace = Constants.Atom_Namespace)]
public class LiveFeed : Feed
{
}
