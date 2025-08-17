using System.Xml.Serialization;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models;

[XmlRoot("object", Namespace = Constants.ActivityStreams_Namespace)]
public class ActivityObject
{
    [XmlElement(ElementName = "object-type", Namespace = Constants.ActivityStreams_Namespace)]
    public string ObjectType { get; set; }

    [XmlElement(ElementName = "id", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public string Id { get; set; }

    [XmlElement(ElementName = "title", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public Content Title { get; set; }

    [XmlElement(ElementName = "content", Namespace = Atom.Constants.ATOM_NAMESPACE)]
    public Content Content { get; set; }
}

[XmlRoot("entry", Namespace = Atom.Constants.ATOM_NAMESPACE)]
public class LiveEntry : Entry
{
    [XmlElement(ElementName = "category")]
    public Category Category { get; set; }

    [XmlElement(ElementName = "generator")]
    public string Generator { get; set; }

    [XmlElement(ElementName = "verb", Namespace = Constants.ActivityStreams_Namespace)]
    public string ActivityVerb { get; set; }

    [XmlElement(ElementName = "object", Namespace = Constants.ActivityStreams_Namespace)]
    public ActivityObject ActivityObject { get; set; }

    [XmlElement(ElementName = "activityId", Namespace = Constants.Live_Namespace)]
    public string ActivityId { get; set; }

    [XmlElement(ElementName = "appId", Namespace = Constants.Live_Namespace)]
    public string AppId { get; set; }

    [XmlElement(ElementName = "changeType", Namespace = Constants.Live_Namespace)]
    public string ChangeType { get; set; }

    [XmlElement(ElementName = "SourceId", Namespace = Constants.Live_Namespace)]
    public string SourceId { get; set; }

    [XmlElement(ElementName = "ServiceActivityId", Namespace = Constants.Live_Namespace)]
    public string ServiceActivityId { get; set; }

    [XmlElement(ElementName = "Reactions", Namespace = Constants.Live_Namespace)]
    public string Reactions { get; set; }
}
