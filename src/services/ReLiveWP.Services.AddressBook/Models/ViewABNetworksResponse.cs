using System.ServiceModel;
using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

[MessageContract]
[XmlRoot(ElementName = "ViewABNetworksResponse", Namespace = AddressBookConstants.Ns)]
public class ViewABNetworksResponse
{
    [MessageHeader]
    public ServiceHeader ServiceHeader { get; set; } = new ServiceHeader();

    [MessageBodyMember]
    [XmlArray("ViewABNetworksResult")]
    [XmlArrayItem("NetworkInfo")]
    public List<NetworkInfo> ViewABNetworksResult { get; set; } = [];
}
