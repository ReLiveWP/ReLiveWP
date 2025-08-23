using System.Xml.Serialization;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("entry", Namespace = Constants.Atom_Namespace)]
public class LiveEntry : Entry
{
    [XmlElement(ElementName = "category")]
    public List<LiveCategory> Categories { get; set; } = [];

    [XmlElement(ElementName = "generator")]
    public string Generator { get; set; } = default!;

    [XmlElement(ElementName = "verb", Namespace = Constants.ActivityStreams_Namespace)]
    public string ActivityVerb { get; set; } = default!;

    [XmlElement(ElementName = "object", Namespace = Constants.ActivityStreams_Namespace)]
    public List<LiveActivityObject> Activities { get; set; } = [];

    [XmlElement(ElementName = "activityId", Namespace = Constants.Live_Namespace)]
    public string ActivityId { get; set; } = default!;

    [XmlElement(ElementName = "appId", Namespace = Constants.Live_Namespace)]
    public string AppId { get; set; } = default!;

    [XmlElement(ElementName = "changeType", Namespace = Constants.Live_Namespace)]
    public string ChangeType { get; set; } = default!;

    [XmlElement(ElementName = "SourceId", Namespace = Constants.Live_Namespace)]
    public string SourceId { get; set; } = default!;

    [XmlElement(ElementName = "ServiceActivityId", Namespace = Constants.Live_Namespace)]
    public string ServiceActivityId { get; set; } = default!;

    [XmlArrayItem("ReplyReaction", Type = typeof(LiveReplyReaction))]
    [XmlArrayItem("RetweetReaction", Type = typeof(LiveRetweetReaction))]
    [XmlArray(ElementName = "Reactions", Namespace = Constants.Live_Namespace, IsNullable =true)]
    public List<LiveReaction> Reactions { get; set; } = default!;
}
