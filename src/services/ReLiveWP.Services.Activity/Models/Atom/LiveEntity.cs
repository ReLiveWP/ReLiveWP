using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("Entity", Namespace = Constants.Live_Namespace)]
public class LiveEntity
{
    public const string UserMentionType = "UserMention";

    [XmlElement("Type", Namespace = Constants.Live_Namespace)]
    public string Type { get; set; } = default!;

    [XmlElement("Start", Namespace = Constants.Live_Namespace)]
    public int Start { get; set; }

    [XmlElement("End", Namespace = Constants.Live_Namespace)]
    public int End { get; set; }

    [XmlElement("User", Namespace = Constants.Live_Namespace)]
    public LiveEntityUser? User { get; set; }
}
