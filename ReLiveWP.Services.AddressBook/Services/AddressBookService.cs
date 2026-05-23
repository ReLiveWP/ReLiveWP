using System.ServiceModel;
using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Services;

public class ABApplicationHeader
{

}
public class ABAuthHeader
{

}


[MessageContract]
public class ViewABNetworks
{
    [MessageHeader]
    public ABApplicationHeader ABApplicationHeader { get; set; } = null!;
    [MessageHeader]
    public ABAuthHeader ABAuthHeader { get; set; } = null!;
}

public class ServiceHeader
{
    [XmlElement("Version", Namespace = "http://www.msn.com/webservices/AddressBook")]
    public string Version { get; set; } = "11.01.0922.0000";
}

[XmlRoot(ElementName = "Annotation", Namespace = "http://www.msn.com/webservices/AddressBook")]
public class Annotation
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Value")]
    public string Value { get; set; }
}


public class NetworkInfo
{
    [XmlArray("Annotations", IsNullable = true)]
    [XmlArrayItem("Annotation")]
    public List<Annotation> Annotations { get; set; } =
        [
            new() { Name = "Live.Network.PSAState", Value = "Accept" }
        ];

    // can be 7, 8, 22	

    [XmlElement("SourceId")]
    public string SourceId { get; set; }

    [XmlElement("DomainId")]
    public int DomainId { get; set; }

    [XmlElement("DomainTag")]
    public string DomainTag { get; set; }

    [XmlElement("UserTileUrl")]
    public string UserTileUrl { get; set; }

    [XmlElement("DisplayName")]
    public string DisplayName { get; set; }
}

[MessageContract]
[XmlRoot(ElementName = "ViewABNetworksResponse", Namespace = "http://www.msn.com/webservices/AddressBook")]
public class ViewABNetworksResponse
{
    [MessageHeader]
    public ServiceHeader ServiceHeader { get; set; } = new ServiceHeader();

    [MessageBodyMember]
    [XmlArray("ViewABNetworksResult")]
    [XmlArrayItem("NetworkInfo")]
    public List<NetworkInfo> ViewABNetworksResult { get; set; } = [];
}

[ServiceContract(Namespace = "http://www.msn.com/webservices/AddressBook")]
public interface IAddressBookService
{
    [OperationContract(Action = nameof(ViewABNetworks))]
    ViewABNetworksResponse ViewABNetworks(ViewABNetworks message);
}

public class AddressBookService : IAddressBookService
{
    public ViewABNetworksResponse ViewABNetworks(ViewABNetworks message)
    {
        return new ViewABNetworksResponse()
        {
            ViewABNetworksResult =
            {
                new NetworkInfo()
                {
                    DomainId = 22,
                    SourceId = "TWITR",
                    DomainTag = "WL",
                    DisplayName = "Wam",
                    //ProfileUrl = "https://bsky.app/profile/wamwoowam.co.uk",
                    UserTileUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                    //CreateDate = DateTime.Today,
                    //LastChanged = DateTime.Now,
                    //RelationshipType = 0,
                    //RelationshipState = 0,
                    //RelationshipRole = 0,
                    //RelationshipStateDate = DateTime.Today,
                }
            }
        };
    }
}
