using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

[XmlRoot(ElementName = "Annotation", Namespace = "http://www.msn.com/webservices/AddressBook")]
public class Annotation
{
    [XmlElement("Name")]
    public required string Name { get; set; }

    [XmlElement("Value")]
    public required string Value { get; set; }
}
