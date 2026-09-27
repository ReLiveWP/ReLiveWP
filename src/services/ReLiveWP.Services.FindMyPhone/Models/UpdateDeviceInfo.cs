using System.Xml.Serialization;

namespace ReLiveWP.Services.FindMyPhone.Models;

[XmlRoot("UpdateDeviceInfoRequest", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateDeviceInfoRequestModel
{
    [XmlArray("Properties")]
    [XmlArrayItem("Property")]
    public List<DeviceProperty> Properties { get; set; } = [];
}


[XmlRoot("UpdateDeviceInfoResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class UpdateDeviceInfoResponseModel
{
    [XmlElement("ResponseCode", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int ResponseCode { get; set; }
    [XmlElement("ResponseMessage", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public string ResponseMessage { get; set; } = "";
    [XmlElement("EnabledForMarket", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int EnabledForMarket { get; set; } = 2;
}
