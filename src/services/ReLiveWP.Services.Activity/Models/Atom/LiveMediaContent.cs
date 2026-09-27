using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

public class LiveMediaContent
{
    [XmlAttribute("url")]
    public string Url { get; set; } = default!;

    [XmlAttribute("duration")]
    public int Duration { get; set; }
    public bool ShouldSerializeDuration() => Duration > 0;
}
