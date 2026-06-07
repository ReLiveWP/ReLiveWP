using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

public class ServiceHeader
{
    [XmlElement("Version", Namespace = "http://www.msn.com/webservices/AddressBook")]
    public string Version { get; set; } = "11.01.0922.0000";
}
