using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using ReLiveWP.Services.AddressBook.Models;

namespace ReLiveWP.Services.AddressBook.Tests;

// WWSAPI on the phone reads NetworkInfo as a strict sequence. The names, the order and every
// REQUIRED element below come from WLProv.dll's own struct description at 0x10014678; get any of
// them wrong and ViewABNetworks fails to parse, SyncAbch bails, and no social store is ever created.
public class NetworkInfoWireFormatTests
{
    private const string Ns = "http://www.msn.com/webservices/AddressBook";

    // in wire order
    private static readonly string[] Schema =
    [
        "Annotations",
        "DomainId",
        "SourceId",
        "DomainTag",
        "UserTileURL",
        "ProfileURL",
        "DisplayName",
        "RelationshipType",
        "RelationshipState",
        "RelationshipStateDate",
        "RelationshipRole",
        "ExtendedData",
        "NDRCount",
        "InviterMessage",
        "InviterCID",
        "InviterName",
        "InviterEmail",
        "CreateDate",
        "LastChanged",
        "PropertiesChanged",
        "ForwardingEmail",
        "Settings",
    ];

    private static readonly string[] Required =
    [
        "DomainId",
        "RelationshipType",
        "RelationshipState",
        "RelationshipStateDate",
        "RelationshipRole",
        "NDRCount",
        "InviterCID",
        "CreateDate",
        "LastChanged",
        "PropertiesChanged",
        "Settings",
    ];

    private static NetworkInfo Twitter() => new()
    {
        DomainId = 22,
        SourceId = "TWITR",
        DomainTag = "TWITR",
        DisplayName = "Wam",
        RelationshipType = 3,
        RelationshipState = 3,
        RelationshipStateDate = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
        CreateDate = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
        LastChanged = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
        Annotations =
        [
            new() { Name = "Live.Network.PSAState", Value = "Accept" },
            new() { Name = "Live.Network.Offers", Value = "2129" },
        ],
    };

    private static XElement Serialize(NetworkInfo network)
    {
        var serializer = new XmlSerializer(typeof(NetworkInfo), new XmlRootAttribute("NetworkInfo") { Namespace = Ns });

        using var buffer = new StringWriter();
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { OmitXmlDeclaration = true }))
            serializer.Serialize(writer, network);

        return XElement.Parse(buffer.ToString());
    }

    [Fact]
    public void Every_element_the_phone_demands_is_present()
    {
        var written = Serialize(Twitter()).Elements().Select(e => e.Name.LocalName).ToList();

        Assert.Empty(Required.Except(written));
    }

    [Fact]
    public void The_elements_come_out_in_schema_order()
    {
        var written = Serialize(Twitter()).Elements().Select(e => e.Name.LocalName).ToList();

        Assert.Equal(Schema.Where(written.Contains), written);
    }

    // an element WWSAPI has no field for fails the struct, so we must not invent any
    [Fact]
    public void Nothing_outside_the_schema_is_written()
    {
        var written = Serialize(Twitter()).Elements().Select(e => e.Name.LocalName);

        Assert.Empty(written.Except(Schema));
    }

    [Fact]
    public void Everything_is_in_the_addressbook_namespace()
    {
        var root = Serialize(Twitter());

        Assert.All(root.Descendants(), e => Assert.Equal(Ns, e.Name.NamespaceName));
    }

    [Fact]
    public void Annotations_are_name_value_pairs_under_a_wrapper()
    {
        var annotations = Serialize(Twitter()).Element(XName.Get("Annotations", Ns));

        Assert.NotNull(annotations);
        Assert.All(annotations.Elements(), e =>
        {
            Assert.Equal("Annotation", e.Name.LocalName);
            Assert.Equal(["Name", "Value"], e.Elements().Select(c => c.Name.LocalName));
        });
    }

    // VarDateFromStr on the device, so the marker has to be a real xs:dateTime
    [Fact]
    public void Dates_are_written_as_utc()
    {
        var written = Serialize(Twitter());

        Assert.Equal("2026-09-28T12:00:00Z", written.Element(XName.Get("CreateDate", Ns))?.Value);
        Assert.Equal("2026-09-28T12:00:00Z", written.Element(XName.Get("LastChanged", Ns))?.Value);
    }

    [Fact]
    public void An_omitted_optional_element_does_not_disturb_the_order()
    {
        var network = Twitter();
        network.SourceId = null;
        network.DisplayName = null;

        var written = Serialize(network).Elements().Select(e => e.Name.LocalName).ToList();

        Assert.DoesNotContain("SourceId", written);
        Assert.Equal(Schema.Where(written.Contains), written);
    }
}
