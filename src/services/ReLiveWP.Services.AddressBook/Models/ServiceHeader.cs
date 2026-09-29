using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

// SoapCore writes response headers with the DataContractSerializer even in XmlSerializer mode,
// which put Version in the schemas.datacontract.org namespace
[DataContract(Name = "ServiceHeader", Namespace = AddressBookConstants.Ns)]
public class ServiceHeader
{
    [DataMember(Name = "Version")]
    [XmlElement("Version", Namespace = AddressBookConstants.Ns)]
    public string Version { get; set; } = "11.01.0922.0000";
}
