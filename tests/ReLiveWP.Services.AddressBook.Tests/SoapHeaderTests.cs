using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using ReLiveWP.Services.AddressBook.Models;

namespace ReLiveWP.Services.AddressBook.Tests;

public class SoapHeaderTests
{
    private const string Ns = AddressBookConstants.Ns;

    // captured off a device's ViewABNetworks request, ticket shortened
    private const string DeviceAuthHeader = """
        <ABAuthHeader xmlns="http://www.msn.com/webservices/AddressBook">
          <ManagedGroupRequest>false</ManagedGroupRequest>
          <TicketToken>t=eyJ.payload.sig&amp;p=</TicketToken>
        </ABAuthHeader>
        """;

    [Fact]
    public void The_ticket_binds_out_of_the_devices_auth_header()
    {
        var serializer = new XmlSerializer(typeof(ABAuthHeader), new XmlRootAttribute("ABAuthHeader") { Namespace = Ns });

        var header = (ABAuthHeader)serializer.Deserialize(new StringReader(DeviceAuthHeader))!;

        Assert.Equal("t=eyJ.payload.sig&p=", header.TicketToken);
    }

    // SoapCore writes response headers with the DataContractSerializer, which had Version
    // landing in schemas.datacontract.org
    [Fact]
    public void The_service_header_version_is_in_the_addressbook_namespace()
    {
        var serializer = new DataContractSerializer(typeof(ServiceHeader));

        using var buffer = new StringWriter();
        using (var writer = XmlWriter.Create(buffer))
            serializer.WriteObject(writer, new ServiceHeader());

        var root = XElement.Parse(buffer.ToString());

        Assert.Equal(XName.Get("ServiceHeader", Ns), root.Name);
        Assert.Equal("11.01.0922.0000", root.Element(XName.Get("Version", Ns))?.Value);
    }
}
