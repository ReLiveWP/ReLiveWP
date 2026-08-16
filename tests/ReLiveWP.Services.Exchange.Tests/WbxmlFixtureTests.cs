using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// Shared golden fixtures, also read by ReLiveWP.Web/packages/eas-client. The bytes are written
// by hand from the WBXML specification rather than produced by either codec, so agreeing with
// them means agreeing with the format - not merely with each other.
public class WbxmlFixtureTests
{
    // Elements ASWBXML renders as base64 text rather than CDATA on the XML side.
    private static readonly HashSet<(string, string)> Base64Rendered =
    [
        ("Email2", "ConversationId"),
        ("Email2", "ConversationIndex"),
        ("ComposeMail", "Mime"),
    ];

    // ASWBXML's only input is XML, and XML cannot express these.
    private static readonly Dictionary<string, string> NotEncodableFromXml = new()
    {
        ["opaque-empty"] = "an element containing an empty text node round-trips through XML as an empty element",
        ["unknown-token"] = "UNKNOWN_TAG_3F is a placeholder, not a tag with a token",
        ["string-table"] = "the encoder always writes an empty string table",
    };

    private static readonly Dictionary<string, string> NotDecodable = new()
    {
        ["string-table"] = "LoadBytes rejects a non-empty string table",
    };

    private static string FixtureDirectory([CallerFilePath] string thisFile = "")
    {
        var repo = Path.GetDirectoryName(Path.GetDirectoryName(thisFile))!;
        return Path.Combine(repo, "ReLiveWP.Web", "packages", "eas-client", "test", "fixtures");
    }

    // The prefix is the lowercased namespace for every code page MS-ASWBXML defines, but
    // WindowsLive is carried as "live", so it has to be looked up rather than derived.
    private static readonly Lazy<Dictionary<string, string>> Prefixes = new(() =>
    {
        var field = typeof(ASWBXML).GetField("codePages",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var pages = (Dictionary<int, ASWBXMLCodePage>)field.GetValue(new ASWBXML())!;

        return pages.Values.ToDictionary(p => p.Namespace, p => p.Xmlns);
    });

    private static string Prefix(string ns) =>
        Prefixes.Value.TryGetValue(ns, out var prefix) ? prefix : ns.ToLowerInvariant();

    public static TheoryData<string> FixtureNames()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(FixtureDirectory(), "*.json").Order())
            data.Add(Path.GetFileNameWithoutExtension(file));
        return data;
    }

    private record Fixture(string Name, string Description, byte[] Bytes, XmlDocument Xml);

    private static Fixture Load(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory(), name + ".json")));
        var root = doc.RootElement;

        var bytes = new List<byte>();
        foreach (var run in root.GetProperty("wbxml").EnumerateArray())
            bytes.AddRange(Bytes(run));

        var xml = new XmlDocument();
        xml.InsertBefore(xml.CreateXmlDeclaration("1.0", "utf-8", null), null);
        xml.AppendChild(Element(xml, root.GetProperty("tree")));

        return new Fixture(
            name,
            root.GetProperty("description").GetString()!,
            [.. bytes],
            xml);
    }

    private static byte[] Bytes(JsonElement spec)
    {
        if (spec.TryGetProperty("hex", out var hex))
            return Convert.FromHexString(hex.GetString()!.Replace(" ", ""));

        if (spec.TryGetProperty("ascii", out var ascii))
            return Encoding.UTF8.GetBytes(ascii.GetString()!);

        var fill = (byte)spec.GetProperty("fill").GetInt32();
        var length = spec.GetProperty("length").GetInt32();
        return [.. Enumerable.Repeat(fill, length)];
    }

    private static XmlElement Element(XmlDocument doc, JsonElement node)
    {
        var ns = node.GetProperty("ns").GetString()!;
        var name = node.GetProperty("name").GetString()!;

        var element = doc.CreateElement(Prefix(ns), name, ns);
        element.Prefix = Prefix(ns);

        foreach (var child in node.GetProperty("children").EnumerateArray())
        {
            if (child.TryGetProperty("text", out var text))
                element.AppendChild(doc.CreateTextNode(text.GetString()!));
            else if (child.TryGetProperty("opaque", out var opaque))
                element.AppendChild(Opaque(doc, ns, name, Bytes(opaque)));
            else
                element.AppendChild(Element(doc, child));
        }

        return element;
    }

    private static XmlNode Opaque(XmlDocument doc, string ns, string name, byte[] payload)
    {
        if (Base64Rendered.Contains((ns, name)))
            return doc.CreateTextNode(Convert.ToBase64String(payload));

        return doc.CreateCDataSection(System.Text.Encoding.UTF8.GetString(payload));
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Decodes_to_the_expected_document(string name)
    {
        if (NotDecodable.ContainsKey(name)) return;

        var fixture = Load(name);

        var decoder = new ASWBXML();
        decoder.LoadBytes(fixture.Bytes);

        Assert.Equal(fixture.Xml.OuterXml, decoder.GetXmlDocument().OuterXml);
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Encodes_to_the_expected_bytes(string name)
    {
        if (NotEncodableFromXml.ContainsKey(name)) return;

        var fixture = Load(name);

        var encoder = new ASWBXML();
        encoder.LoadXml(fixture.Xml.OuterXml);

        Assert.Equal(Convert.ToHexString(fixture.Bytes), Convert.ToHexString(encoder.GetBytes()));
    }
}
