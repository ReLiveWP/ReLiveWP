using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Atom.Attributes;

namespace Atom.Xml
{
    [XmlRoot(ElementName = "entry", Namespace = Constants.ATOM_NAMESPACE)]
    [NamespacePrefix("a", Constants.ATOM_NAMESPACE)]
    public class Entry
    {

#if NETSTANDARD
        [System.ComponentModel.DataAnnotations.Key]
#endif
        [XmlElement(ElementName = "id")]
        public string Id { get; set; }

        [XmlElement(ElementName = "summary")]
        public Content Summary { get; set; }

        [XmlElement(ElementName = "title")]
        public Content Title { get; set; } = "List Of Items";

        [XmlIgnore]
        public DateTime? Updated { get; set; }

        [XmlElement(ElementName = "updated")]
        public string UpdatedString
        {
            get => Updated.HasValue ? Updated.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : null;
            set => Updated = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        [XmlIgnore]
        public DateTime? Published { get; set; }

        [XmlElement(ElementName = "published")]
        public string PublishedString
        {
            get => Published.HasValue ? Published.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : null;
            set => Published = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        [XmlElement(ElementName = "author")]
        public Author Author { get; set; }

        [XmlElement(ElementName = "link")]
        public List<Link> Links { get; set; } = new List<Link>();

        [XmlElement(ElementName = "content")]
        public Content Content { get; set; }
    }
}