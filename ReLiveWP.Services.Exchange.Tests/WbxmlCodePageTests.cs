using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

public class WbxmlCodePageTests
{
    private static byte[] Encode<T>(T value) where T : class
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(T)).Serialize(writer, value);

        var encoder = new ASWBXML();
        encoder.LoadXml(writer.ToString());
        return encoder.GetBytes();
    }

    private static XmlElement Decode(byte[] wbxml)
    {
        var decoder = new ASWBXML();
        decoder.LoadBytes(wbxml);
        return decoder.GetXmlDocument().DocumentElement!;
    }

    private static string? Value(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == localName)?.InnerText;

    [Fact]
    public void Calendar_Timezone_survives_wbxml_roundtrip()
    {
        const string tz = "xP///yhHTVQrMDA6MDApAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        var root = Decode(Encode(new CalendarData { Timezone = tz }));

        Assert.Equal(tz, Value(root, "Timezone"));
    }

    [Fact]
    public void Calendar_Timezone_does_not_encode_as_the_unknown_token_sentinel()
    {
        var bytes = Encode(new CalendarData { Timezone = "abc" });

        Assert.DoesNotContain((byte)0xFF, bytes);
    }

    // An element with no token in the current page must fail loudly rather than emit 0xFF.
    [Fact]
    public void Unknown_element_throws_instead_of_emitting_a_sentinel_token()
    {
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateElement("Sync", Constants.AirSync));
        doc.DocumentElement!.AppendChild(doc.CreateElement("NotARealTag", Constants.AirSync));

        var encoder = new ASWBXML();
        encoder.LoadXml(doc.OuterXml);

        var ex = Assert.Throws<InvalidDataException>(() => encoder.GetBytes());
        Assert.Contains("NotARealTag", ex.Message);
    }

    // Code page 3 (AirNotify) defines no tokens. It used to carry an empty namespace, which made it
    // the match for any element whose namespace was missing - so a model that forgot its Namespace=
    // argument routed to a page with no tokens and silently produced 0xFF for every element.
    [Fact]
    public void Element_with_no_namespace_does_not_silently_route_to_the_empty_code_page()
    {
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateElement("Sync", Constants.AirSync));
        doc.DocumentElement!.AppendChild(doc.CreateElement("Orphan"));

        var encoder = new ASWBXML();
        encoder.LoadXml(doc.OuterXml);

        Assert.Throws<InvalidDataException>(() => encoder.GetBytes());
    }

    // MS-ASCMD 2.2.3.109: the Mime element travels as an opaque BLOB. Encoded as an inline string
    // instead, every byte >= 0x80 came back re-encoded and the message was corrupted.
    [Fact]
    public void ComposeMail_Mime_roundtrips_eight_bit_bytes_as_opaque()
    {
        byte[] raw = [0x46, 0x72, 0x6F, 0x6D, 0x3A, 0x20, 0xC3, 0xA9, 0x80, 0xFE, 0x0D, 0x0A];
        var mime = Convert.ToBase64String(raw);

        var root = Decode(Encode(new SendMailRequest { ClientId = "c1", Mime = mime }));

        Assert.Equal(raw, Convert.FromBase64String(Value(root, "Mime")!));
    }

    [Fact]
    public void ComposeMail_Mime_is_not_inline_string_encoded()
    {
        byte[] raw = [0x41, 0x42, 0x43];
        var bytes = Encode(new SendMailRequest { Mime = Convert.ToBase64String(raw) });

        // the base64 text itself must never appear on the wire - only the decoded bytes do
        Assert.DoesNotContain(Convert.ToBase64String(raw), System.Text.Encoding.ASCII.GetString(bytes));
    }
}
