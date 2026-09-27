using System.ServiceModel;
using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

[MessageContract]
[XmlRoot(ElementName = "ViewABNetworksResponse", Namespace = "http://www.msn.com/webservices/AddressBook")]
public class ViewABNetworksResponse
{
    [MessageHeader]
    public ServiceHeader ServiceHeader { get; set; } = new ServiceHeader();

    [MessageBodyMember(Name = "ViewABNetworksResult")]
    public ViewABNetworksResult Result { get; set; } = new();
}
