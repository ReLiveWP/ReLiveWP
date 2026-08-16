using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

public class LiveMediaThumbnail
{
    [XmlAttribute("url")]
    public string Url { get; set; } = default!;
    public bool ShouldSerializeUrl() => Url != null;

    [XmlAttribute("resourceId", Namespace = Constants.Live_Namespace)]
    public string ResourceId { get; set; } = default!;
    public bool ShouldSerializeResourceId() => ResourceId != null;

    [XmlAttribute("maxWidth")]
    public int MaxWidth { get; set; }
    public bool ShouldSerializeMaxWidth() => MaxWidth > 0;

    [XmlAttribute("width")]
    public int Width { get; set; }
    public bool ShouldSerializeWidth() => Width > 0;

    [XmlAttribute("height")]
    public int Height { get; set; }
    public bool ShouldSerializeHeight() => Height > 0;
}
