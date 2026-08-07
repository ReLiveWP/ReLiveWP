using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

public class LiveMediaThumbnail
{
    [XmlAttribute("url")]
    public string Url { get; set; } = default!;

    [XmlAttribute("maxWidth")]
    public int MaxWidth { get; set; }

    [XmlAttribute("width")]
    public int Width { get; set; }
    public bool ShouldSerializeWidth() => Width > 0;

    [XmlAttribute("height")]
    public int Height { get; set; }
    public bool ShouldSerializeHeight() => Height > 0;
}
