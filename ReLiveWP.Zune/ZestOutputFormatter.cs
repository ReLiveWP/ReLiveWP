using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ReLiveWP.Zune;

public class ZestOutputFormatter : XmlSerializerOutputFormatter
{
    private Dictionary<Type, XmlSerializerNamespaces> _namespaceCache
        = new Dictionary<Type, XmlSerializerNamespaces>();

    public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
    {
        return base.GetSupportedContentTypes(contentType, objectType)!;
    }

    protected override void Serialize(XmlSerializer xmlSerializer, XmlWriter xmlWriter, object? value)
    {
        if (value is null)
        {
            base.Serialize(xmlSerializer, xmlWriter, value);
            return;
        }

        var type = value.GetType();
        if (!_namespaceCache.TryGetValue(type, out var ns))
        {
            ns = new XmlSerializerNamespaces();

            var attributes = type.GetCustomAttributes<NamespacePrefixAttribute>(true);
            foreach (var attribute in attributes)
                ns.Add(attribute.Prefix, attribute.Namespace);

            _namespaceCache[type] = ns;
        }

        xmlSerializer.Serialize(xmlWriter, value, ns);
    }
}
