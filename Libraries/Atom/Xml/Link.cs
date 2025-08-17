using System;
using System.Xml.Serialization;

namespace Atom.Xml
{
    [XmlRoot(ElementName = "link", Namespace = Constants.ATOM_NAMESPACE)]
    public class Link
    {
        public Link() { }

        public Link(string href, string relation = "self", string type = Constants.ATOM_MIMETYPE)
        {
            Href = href;
            Relation = relation;
            Type = type;
        }

        [XmlAttribute(AttributeName = "rel")]
        public string Relation { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "title")]
        public string Title { get; set; }

        //[XmlAttribute(AttributeName = "updated")]
        //public string Updated { get; set; }

        [XmlIgnore]
        public DateTime? Updated { get; set; }

        [XmlAttribute(AttributeName = "updated")]
        public string UpdatedString
        {
            get => Updated.HasValue ? Updated.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : null;
            set => Updated = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        [XmlAttribute(AttributeName = "id")]
#if NETSTANDARD
        [System.ComponentModel.DataAnnotations.Key]
#endif
        public string Id { get; set; }

        public static implicit operator Link(string href)
        {
            return new Link(href);
        }
    }
}