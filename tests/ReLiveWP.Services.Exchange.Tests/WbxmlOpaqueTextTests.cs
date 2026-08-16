using System.Text;
using System.Xml;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// STR_I was pinned to UTF-8 (see WbxmlCodecTests) but the opaque path was not: a text element
// transmitted as an OPAQUE run came back byte-widened, so a contact named "Síoda" stored as
// "SÃ­oda". Encode and decode were corrupt in the same direction, so the DEBUG round-trip verify
// in ActiveSyncCommandController could not see it.
public class WbxmlOpaqueTextTests
{
    private const byte Opaque = 0xC3;
    private const byte End = 0x01;
    private const byte SwitchPage = 0x00;

    private const int ContactsPage = 1;
    private const byte FirstNameToken = 0x1F;
    private const byte MimeToken = 0x10;
    private const int ComposeMailPage = 21;

    private static byte[] Document(int page, byte token, byte[] payload)
    {
        List<byte> doc = [0x03, 0x01, 0x6A, 0x00, SwitchPage, (byte)page, (byte)(token | 0x40), Opaque];
        doc.AddRange(ASWBXML.EncodeMultiByteInteger(payload.Length));
        doc.AddRange(payload);
        doc.Add(End);
        return [.. doc];
    }

    private static XmlElement Decode(byte[] wbxml)
    {
        var decoder = new ASWBXML();
        decoder.LoadBytes(wbxml);
        return decoder.GetXmlDocument().DocumentElement!;
    }

    [Theory]
    [InlineData("Síoda")]
    [InlineData("café")]
    [InlineData("Здравствуйте")]
    [InlineData("東京タワー")]
    [InlineData("family 👨‍👩‍👧 emoji")]
    public void Opaque_text_decodes_as_utf8(string name)
    {
        var root = Decode(Document(ContactsPage, FirstNameToken, Encoding.UTF8.GetBytes(name)));

        Assert.Equal("FirstName", root.LocalName);
        Assert.Equal(name, root.InnerText);
    }

    [Fact]
    public void Opaque_text_reencodes_as_utf8()
    {
        var wbxml = Document(ContactsPage, FirstNameToken, Encoding.UTF8.GetBytes("Síoda"));

        var encoder = new ASWBXML();
        encoder.LoadXml(Decode(wbxml).OwnerDocument!.OuterXml);

        Assert.Equal(Convert.ToHexString(wbxml), Convert.ToHexString(encoder.GetBytes()));
    }

    // ComposeMail:Mime is a genuine byte channel: it must stay bytes, not be handed to a decoder
    // that would replace anything invalid with U+FFFD.
    [Fact]
    public void Binary_opaque_elements_stay_bytes()
    {
        byte[] payload = [0x00, 0xFF, 0xFE, 0x80, 0x41];

        var root = Decode(Document(ComposeMailPage, MimeToken, payload));

        Assert.Equal("Mime", root.LocalName);
        Assert.Equal(Convert.ToBase64String(payload), root.InnerText);
    }
}
