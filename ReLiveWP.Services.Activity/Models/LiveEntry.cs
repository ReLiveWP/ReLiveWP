using System.Xml.Serialization;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models;

[XmlRoot("object", Namespace = Constants.ActivityStreams_Namespace)]
public class ActivityObject
{
    [XmlElement(ElementName = "object-type", Namespace = Constants.ActivityStreams_Namespace)]
    public string ObjectType { get; set; } = default!;

    [XmlElement(ElementName = "id", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public string Id { get; set; } = default!;

    [XmlElement(ElementName = "title", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public Content Title { get; set; } = default!;

    [XmlElement(ElementName = "content", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public Content Content { get; set; } = default!;

    [XmlElement(ElementName = "link", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public List<Link> Links { get; set; } = [];
}

[XmlRoot("entry", Namespace = Atom.Constants.ATOM_NAMESPACE)]
public class LiveEntry : Entry
{
    [XmlElement(ElementName = "category")]
    public List<Category> Categories { get; set; } = [];

    [XmlElement(ElementName = "generator")]
    public string Generator { get; set; } = default!;

    [XmlElement(ElementName = "verb", Namespace = Constants.ActivityStreams_Namespace)]
    public string ActivityVerb { get; set; } = default!;

    [XmlElement(ElementName = "object", Namespace = Constants.ActivityStreams_Namespace)]
    public List<ActivityObject> Activities { get; set; } = [];

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

    [XmlElement(ElementName = "Reactions", Namespace = Constants.Live_Namespace)]
    public string Reactions { get; set; } = default!;
}
