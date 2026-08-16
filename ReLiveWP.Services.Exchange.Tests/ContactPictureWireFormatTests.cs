using Google.Protobuf;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// MS-ASCNTC 2.2.2.58 types Picture as a string, and MS-ASDTYPE 2.7 says string elements MUST be
// WBXML inline strings. Only 2.7.1 byte arrays are opaque, and MS-ASCNTC has none. So Picture goes
// out as base64 text, and adding it to ASWBXML's opaque set would break a working wire format.
public class ContactPictureWireFormatTests
{
    private const byte StrI = 0x03;
    private const byte Opaque = 0xC3;

    private static byte[] Encode(byte[] picture)
    {
        var contact = new ContactItem { FirstName = "Wam", Picture = ByteString.CopyFrom(picture) };
        var appData = contact.ToApplicationData();

        var encoder = new ASWBXML();
        encoder.LoadXml(Serialize(appData));
        return encoder.GetBytes();
    }

    private static string Serialize(ReLiveWP.Services.Exchange.Models.ApplicationData appData)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ReLiveWP.Services.Exchange.Models.ApplicationData));
        using var writer = new StringWriter();
        serializer.Serialize(writer, appData);
        return writer.ToString();
    }

    [Fact]
    public void Picture_is_an_inline_string_not_an_opaque_run()
    {
        byte[] picture = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        var wbxml = Encode(picture);

        Assert.DoesNotContain(Opaque, wbxml);

        var expected = System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(picture));
        Assert.Contains(Convert.ToHexString(expected), Convert.ToHexString(wbxml));
    }

    [Fact]
    public void Picture_round_trips_through_the_codec()
    {
        byte[] picture = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

        var decoder = new ASWBXML();
        decoder.LoadBytes(Encode(picture));

        var elements = decoder.GetXmlDocument()
            .GetElementsByTagName("Picture", ReLiveWP.Services.Exchange.Models.Constants.Contacts);

        Assert.Equal(1, elements.Count);
        Assert.Equal(picture, Convert.FromBase64String(elements[0]!.InnerText));
    }

    [Fact]
    public void Inline_string_marker_precedes_the_picture_payload()
    {
        var wbxml = Encode([0x01, 0x02, 0x03]);
        var payload = System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03 }));

        var index = IndexOf(wbxml, payload);
        Assert.True(index > 0, "picture payload not found in the encoded document");
        Assert.Equal(StrI, wbxml[index - 1]);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length && match; j++)
                match = haystack[i + j] == needle[j];

            if (match) return i;
        }

        return -1;
    }
}
