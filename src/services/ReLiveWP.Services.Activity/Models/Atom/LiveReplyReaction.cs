using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot("ReplyReaction", Namespace = Constants.Live_Namespace)]
public class LiveReplyReaction : LiveReaction
{
    [XmlAttribute(AttributeName = "CanReact")]
    public bool CanReact { get; set; } = true;

    [XmlAttribute(AttributeName = "Count")]
    public int Count { get; set; } = 0;

    [XmlAttribute(AttributeName = "Prefix")]
    public string? Prefix { get; set; }

    //[XmlAttribute(AttributeName = "TextLimit")]
    //public int TextLimit { get; set; } = 300;

    [XmlArray(ElementName = "Replies", Namespace = Constants.Live_Namespace, IsNullable = true)]
    public List<LiveReply> Replies { get; set; } = [];
}
