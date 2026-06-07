using System.ServiceModel;
using ReLiveWP.Services.AddressBook.Models;

namespace ReLiveWP.Services.AddressBook.Services;

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
            Result =
            {
                Networks =
                {
                    new NetworkInfo()
                    {
                        DomainId = 22,
                        SourceId = "TWITR",
                        DomainTag = "WL",
                        DisplayName = "Wam",
                        ProfileURL = "https://bsky.app/profile/wamwoowam.co.uk",
                        UserTileURL = "https://cdn.bsky.app/img/avatar/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreihkzoksalhxgsivjew4xbftdnsa27bcc5xcl5vf5opaou2eswtsda@jpeg",
                        CreateDate = now,
                        LastChanged = now,
                        RelationshipStateDate = now,
                        Annotations =
                        [
                            new() { Name = "Live.Network.PSAState", Value = "Accept" }
                        ]
                    }
                }
            }
        };
    }
}
