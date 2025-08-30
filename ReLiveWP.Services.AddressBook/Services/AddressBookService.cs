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

public class Annotation
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public class NetworkInfo
{
    public List<Annotation> Annotations { get; set; } =
        [
            new() { Name = "Live.Network.PSAState", Value = "Accept" }
        ];

    // can be 7, 8, 22
    public required int DomainId { get; set; }
    public required string SourceId { get; set; }
    public required string DomainTag { get; set; }
    public required string UserTileUrl { get; set; }
    public required string ProfileUrl { get; set; }
    public required string DisplayName { get; set; }
    public required int RelationshipType { get; set; }
    public required int RelationshipState { get; set; }
    public required DateTime RelationshipStateDate { get; set; }
    public required long RelationshipRole { get; set; }
    public string ExtendedData { get; set; } = string.Empty;
    public int NDRCount { get; set; }
    public string InviterMessage { get; set; } = string.Empty;
    public long InviterCID { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public string InviterEmail { get; set; } = string.Empty;
    public required DateTime CreateDate { get; set; }
    public required DateTime LastChanged { get; set; }
    public string PropertiesChanged { get; set; } = string.Empty; // << todo: what is this
    public string ForwardingEmail { get; set; } = string.Empty;
    public int Settings { get; set; }
}

public class ViewABNetworksResult
{
    [XmlElement(ElementName = "NetworkInfo")]
    public List<NetworkInfo> NetworkInfo { get; set; } = [];
}

[MessageContract]
public class ViewABNetworksResponse
{
    [MessageHeader]
    public ServiceHeader ServiceHeader { get; set; } = new ServiceHeader();

    [MessageBodyMember]
    [XmlElement(ElementName = "ViewABNetworksResult")]
    public ViewABNetworksResult ViewABNetworksResult { get; set; }
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
            ViewABNetworksResult = new ViewABNetworksResult()
            {
                NetworkInfo =
                {
                    new NetworkInfo()
                    {
                        DomainId = 7,
                        SourceId = "WL",
                        DomainTag = "WL",
                        DisplayName = "Wam",
                        ProfileUrl = "https://bsky.app/profile/wamwoowam.co.uk",
                        UserTileUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                        CreateDate = DateTime.Today,
                        LastChanged = DateTime.Now,
                        RelationshipType = 0,
                        RelationshipState = 0,
                        RelationshipRole = 0,
                        RelationshipStateDate = DateTime.Today,
                    },
                    new NetworkInfo()
                    {
                        DomainId = 8,
                        SourceId = "WL",
                        DomainTag = "WL",
                        DisplayName = "Wam",
                        ProfileUrl = "https://bsky.app/profile/wamwoowam.co.uk",
                        UserTileUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                        CreateDate = DateTime.Today,
                        LastChanged = DateTime.Now,
                        RelationshipType = 0,
                        RelationshipState = 0,
                        RelationshipRole = 0,
                        RelationshipStateDate = DateTime.Today,
                    },
                        new NetworkInfo()
                    {
                        DomainId = 22,
                        SourceId = "WL",
                        DomainTag = "WL",
                        DisplayName = "Wam",
                        ProfileUrl = "https://bsky.app/profile/wamwoowam.co.uk",
                        UserTileUrl = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                        CreateDate = DateTime.Today,
                        LastChanged = DateTime.Now,
                        RelationshipType = 0,
                        RelationshipState = 0,
                        RelationshipRole = 0,
                        RelationshipStateDate = DateTime.Today,
                    }
                }
            }
        };
    }
}
