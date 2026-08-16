using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Atom.Attributes;

namespace ReLiveWP.Services.Activity.Tests;

internal static class AtomSerialization
{
    // mirrors AtomOutputFormatter, which reads prefix attributes off the root type only
    public static string Serialize(object value)
    {
        var namespaces = new XmlSerializerNamespaces();
        foreach (var attribute in value.GetType().GetCustomAttributes<NamespacePrefixAttribute>(true))
            namespaces.Add(attribute.Prefix, attribute.Namespace);

        var buffer = new StringBuilder();
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { Encoding = Encoding.UTF8 }))
            new XmlSerializer(value.GetType()).Serialize(writer, value, namespaces);

        return buffer.ToString();
    }
}
