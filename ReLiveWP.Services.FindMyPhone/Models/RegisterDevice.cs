using System.Xml.Serialization;

namespace ReLiveWP.Services.FindMyPhone.Models;

[XmlRoot("RegisterDeviceRequest", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class RegisterDeviceRequestModel
{
    [XmlArray("Properties")]
    [XmlArrayItem("Property")]
    public List<DeviceProperty> Properties { get; set; } = [];
}

public class DeviceProperty
{
    [XmlElement("Name")]
    public string Name { get; set; } = "";

    [XmlElement("Value")]
    public string Value { get; set; } = "";
}

[XmlRoot("RegisterDeviceResponse", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
public class RegisterDeviceResponseModel
{
    [XmlElement("ResponseCode", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public int ResponseCode { get; set; }
    [XmlElement("ResponseMessage", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public string ResponseMessage { get; set; } = "";
    [XmlElement("EnabledForMarket", Namespace = "http://schemas.microsoft.com/WindowsPhone/Data/2010/09")]
    public bool EnabledForMarket { get; set; }
}
