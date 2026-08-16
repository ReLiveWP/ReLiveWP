using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// Every element any model can emit must have a token in its code page. Without this, a model
// property whose element is missing from the table (or spelled differently) encodes to the 0xFF
// sentinel and ships an undecodable stream - which is how both calendar:Timezone (wrong case) and
// calendar:BodyTruncated (2.5-only, no token) went unnoticed.
public class WbxmlTokenCoverageTests
{
    private static IEnumerable<(string Namespace, string LocalName, string Declaring)> DeclaredElements()
    {
        var assembly = typeof(ReLiveWP.Services.Exchange.Models.EasCommand).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace != "ReLiveWP.Services.Exchange.Models") continue;

            foreach (var root in type.GetCustomAttributes<XmlRootAttribute>())
                if (!string.IsNullOrEmpty(root.Namespace) && !string.IsNullOrEmpty(root.ElementName))
                    yield return (root.Namespace, root.ElementName, type.Name);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                foreach (var el in prop.GetCustomAttributes<XmlElementAttribute>())
                    if (!string.IsNullOrEmpty(el.Namespace) && !string.IsNullOrEmpty(el.ElementName))
                        yield return (el.Namespace, el.ElementName, $"{type.Name}.{prop.Name}");
        }
    }

    public static TheoryData<string, string, string> AllDeclaredElements()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var (ns, name, declaring) in DeclaredElements().Distinct())
            data.Add(ns, name, declaring);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllDeclaredElements))]
    public void Every_model_element_has_a_wbxml_token(string ns, string localName, string declaring)
    {
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateElement(localName, ns));

        var encoder = new ASWBXML();
        encoder.LoadXml(doc.OuterXml);

        var ex = Record.Exception(() => encoder.GetBytes());

        Assert.True(ex is null,
            $"{declaring} emits <{localName} xmlns=\"{ns}\"> but no code page defines a token for it: {ex?.Message}");
    }
}
