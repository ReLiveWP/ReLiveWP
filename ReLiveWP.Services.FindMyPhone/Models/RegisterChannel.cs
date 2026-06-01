using System.Xml.Serialization;

namespace ReLiveWP.Services.FindMyPhone.Models;

[XmlRoot("RegisterChannelRequest", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class RegisterChannelRequestModel
{
    [XmlElement]
    public string NotificationUri { get; set; }
    [XmlElement]
    public int SecretKeyId { get; set; }
    [XmlElement]
    public bool OOBE { get; set; }
}


[XmlRoot("RegisterChannelResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class RegisterChannelResponseModel
{
    [XmlElement("ResponseCode", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int ResponseCode { get; set; }
    [XmlElement("ResponseMessage", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public string ResponseMessage { get; set; } = "";
}
