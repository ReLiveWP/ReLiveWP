using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Move = "Move";
}

[XmlRoot("MoveItems", Namespace = Constants.Move)]
public class MoveItems
{
    [XmlElement("Move", Namespace = Constants.Move)]
    public List<MoveItem> Moves { get; set; } = [];
}

public class MoveItem
{
    [XmlElement("SrcMsgId", Namespace = Constants.Move)]
    public string SrcMsgId { get; set; } = string.Empty;

    [XmlElement("SrcFldId", Namespace = Constants.Move)]
    public string SrcFldId { get; set; } = string.Empty;

    [XmlElement("DstFldId", Namespace = Constants.Move)]
    public string DstFldId { get; set; } = string.Empty;
}

[XmlRoot("MoveItems", Namespace = Constants.Move)]
public class MoveItemsResponse
{
    [XmlElement("Response", Namespace = Constants.Move)]
    public List<MoveResponse> Responses { get; set; } = [];
}

public class MoveResponse
{
    [XmlElement("SrcMsgId", Namespace = Constants.Move)]
    public string SrcMsgId { get; set; } = string.Empty;

    // 1=invalid source, 2=invalid dest, 3=success, 4=src==dst
    [XmlElement("Status", Namespace = Constants.Move)]
    public int Status { get; set; }

    [XmlElement("DstMsgId", Namespace = Constants.Move)]
    public string? DstMsgId { get; set; }

    public bool ShouldSerializeDstMsgId() => DstMsgId is not null;
}
