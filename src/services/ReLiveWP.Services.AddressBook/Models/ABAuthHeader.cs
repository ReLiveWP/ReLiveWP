using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

public class ABAuthHeader
{
    [XmlElement("TicketToken")] public string? TicketToken { get; set; }
}
