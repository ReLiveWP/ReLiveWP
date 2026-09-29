using System.ServiceModel;

namespace ReLiveWP.Services.AddressBook.Models;

[MessageContract]
public class ViewABNetworks
{
    [MessageHeader(Name = "ABApplicationHeader", Namespace = AddressBookConstants.Ns)]
    public ABApplicationHeader? ABApplicationHeader { get; set; }

    [MessageHeader(Name = "ABAuthHeader", Namespace = AddressBookConstants.Ns)]
    public ABAuthHeader? ABAuthHeader { get; set; }
}
