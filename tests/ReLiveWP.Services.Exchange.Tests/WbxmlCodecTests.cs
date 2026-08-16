using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// The codec advertises UTF-8 (charset 0x6A) but historically encoded/decoded one byte per char
// (Latin-1): U+0080-U+00FF became a lone high byte and anything above U+00FF lost its high bytes
// entirely, silently corrupting contact/email text. These pin STR_I inline text to real UTF-8.
public class WbxmlCodecTests
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

    private static bool Contains(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= haystack.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { match = false; break; }
            if (match) return true;
        }
        return false;
    }

    // CJK/emoji are above U+00FF, so Latin-1 truncation would destroy them and this would fail;
    // the accented cases guard the U+0080-U+00FF range that a corrupt-in/corrupt-out echo can mask.
    [Theory]
    [InlineData("café")]
    [InlineData("München")]
    [InlineData("José García")]
    [InlineData("Здравствуйте")]
    [InlineData("東京タワー")]
    [InlineData("family 👨‍👩‍👧 emoji")]
    public void NonAscii_text_survives_wbxml_roundtrip(string name)
    {
        var request = new FolderCreate { SyncKey = "1", ParentId = "0", DisplayName = name, Type = FolderType.Mail };

        var root = Decode(Encode(request));

        Assert.Equal(name, Value(root, "DisplayName"));
    }

    [Fact]
    public void Accented_text_is_utf8_on_the_wire()
    {
        // "é" is U+00E9: UTF-8 encodes it as the two bytes C3 A9 (Latin-1 would emit a lone E9).
        var bytes = Encode(new FolderCreate { SyncKey = "1", ParentId = "0", DisplayName = "é", Type = FolderType.Mail });

        Assert.True(Contains(bytes, [0xC3, 0xA9]), "DisplayName 'é' must be UTF-8 (C3 A9) on the wire");
    }
}
