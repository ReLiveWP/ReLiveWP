using System.Xml.Serialization;

namespace ReLiveWP.Services.FindMyPhone.Models;

[XmlRoot("UpdateCommandStatusRequest", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateCommandStatusRequestModel
{
    [XmlElement("RequestId")]
    public string RequestId { get; set; } = "";
    [XmlElement("Result")]
    public string Result { get; set; } = "";
    [XmlElement("Final")]
    public bool Final { get; set; }
    [XmlElement("Data")]
    public string? Data { get; set; }
}

[XmlRoot("UpdateCommandStatusBatchedRequest", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateCommandStatusBatchedRequestModel
{
    [XmlArray("Requests")]
    [XmlArrayItem("UpdateCommandStatusRequest")]
    public List<UpdateCommandStatusRequestModel> Requests { get; set; } = [];
}

[XmlRoot("UpdateCommandStatusResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateCommandStatusResponseModel
{
    [XmlElement("ResponseCode", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int ResponseCode { get; set; }
    [XmlElement("ResponseMessage", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public string ResponseMessage { get; set; } = "";
}

[XmlRoot("UpdateCommandStatusBatchedResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateCommandStatusBatchedResponseModel
{
    [XmlArray("Responses", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    [XmlArrayItem("UpdateCommandStatusResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public List<CommandStatusResponseModel> Responses { get; set; } = [];
}

public class CommandStatusResponseModel
{
    [XmlElement("ResponseCode", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int ResponseCode { get; set; }
    [XmlElement("RequestId", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public string RequestId { get; set; } = "";
}
