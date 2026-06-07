using System.ServiceModel;

namespace ReLiveWP.Services.AddressBook.Models;

[MessageContract]
public class ViewABNetworks
{
    [MessageHeader]
    public ABApplicationHeader ABApplicationHeader { get; set; } = null!;
    [MessageHeader]
    public ABAuthHeader ABAuthHeader { get; set; } = null!;
}
