using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Atom.Attributes;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Atom.Formatters;

public class AtomOutputFormatter : XmlSerializerOutputFormatter
{
    public AtomOutputFormatter() : base(new XmlWriterSettings()
    {
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = false
    })
    {
    }

    private ConcurrentDictionary<Type, XmlSerializerNamespaces> _namespaceCache
        = new ConcurrentDictionary<Type, XmlSerializerNamespaces>();
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
