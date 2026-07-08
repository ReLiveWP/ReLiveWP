using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class SOAPUserHeader
{
    [XmlElement("TicketToken")] public string? TicketToken { get; set; }
}
