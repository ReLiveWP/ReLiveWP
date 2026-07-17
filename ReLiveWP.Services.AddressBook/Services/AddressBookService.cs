using System.ServiceModel;
using System.Xml.Serialization;
using ReLiveWP.Services.AddressBook.Models;

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

[XmlRoot(Namespace = "http://www.msn.com/webservices/AddressBook")]
public class ServiceHeader
{
    [XmlElement("Version", Namespace = "http://www.msn.com/webservices/AddressBook")]
    public string Version { get; set; } = "11.01.0922.0000";
}

[XmlRoot(ElementName = "Annotation", Namespace = "http://www.msn.com/webservices/AddressBook")]
public class Annotation
{
    [XmlElement("Name")]
    public string Name { get; set; } = null!;

    [XmlElement("Value")]
    public string Value { get; set; } = null!;
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
    public string SourceId { get; set; } = null!;

    [XmlElement("DomainId")]
    public int DomainId { get; set; }

    [XmlElement("DomainTag")]
    public string DomainTag { get; set; } = null!;

    [XmlElement("UserTileUrl")]
    public string UserTileUrl { get; set; } = null!;

    [XmlElement("DisplayName")]
    public string DisplayName { get; set; } = null!;
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
        var now = DateTime.UtcNow;
        return new ViewABNetworksResponse()
        {
            ViewABNetworksResult =
            {
                new NetworkInfo()
                {
                    DomainId = 22,
                    SourceId = "TWITR",
                    DomainTag = "TWITR",
                    DisplayName = "Wam",
                    // ProfileURL = "https://bsky.app/profile/wamwoowam.co.uk",
                    // UserTileURL = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                    // CreateDate = now,
                    // LastChanged = now,
                    // RelationshipStateDate = now,
                    Annotations =
                    [
                        new() { Name = "Live.Network.PSAState", Value = "Accept" },
                        // TODO: this parses with LOCALE_SYSTEM_DEFAULT on device, which feels like the footgun of all time
                        new() { Name = "Live.Network.LastSync", Value = DateTime.Now.ToString() },
                        // Capability bitmask, TODO: figure out what this means
                        new() { Name = "Live.Network.Offers", Value = "2129" }
                    ]
                }
            }
        };
    }
}
