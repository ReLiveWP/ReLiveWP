using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Atom.Attributes;

namespace Atom.Xml
{
    [XmlRoot("root", Namespace = Constants.ATOM_NAMESPACE)]
    //[NamespacePrefix("a", Constants.ATOM_NAMESPACE)]
    public abstract class Root
    {
        [XmlAttribute(AttributeName = "xmlns")]
        public string Namespace { get; set; }

        [XmlElement(ElementName = "link")]
        public List<Link> Links { get; set; } = new List<Link>();

        [XmlElement(ElementName = "updated")]
        public DateTime Updated { get; set; }

        [XmlElement(ElementName = "title")]
        public Content Title { get; set; }

        [XmlElement(ElementName = "content")]
        public Content Content { get; set; }

        [XmlElement(ElementName = "id")]
#if NETSTANDARD
        [System.ComponentModel.DataAnnotations.Key]
#endif
        public string Id { get; set; }

        [XmlElement(ElementName = "author")]
        public Author Author { get; set; }

        [XmlElement(ElementName = "rights")]
        public string Rights { get; set; }
    }
}
