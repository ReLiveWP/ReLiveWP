using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

public class ViewABNetworksResult
{
    [XmlElement("NetworkInfo")]
    public List<NetworkInfo> Networks { get; set; } = [];
}
