using System.Xml.Serialization;

namespace Atom.Xml
{
    public class Content
    {
        [XmlAttribute(AttributeName = "type")]
        public ContentType Type { get; set; } = ContentType.Text;

        [XmlText]
        public string Value { get; set; }

        public static implicit operator Content(string val)
        {
            return new Content
            {
                Value = val,
                Type = ContentType.Text
            };
        }

        public override string ToString() => Value;
    }

    public enum ContentType
    {
        /// <summary>
        /// Plain text with no entity-escaped HTML.
        /// </summary>
        [XmlEnum("text")]
        Text,

        /// <summary>
        /// Entity-escaped HTML.
        /// </summary>
        [XmlEnum("html")]
        HTML,

        /// <summary>
        /// Inline XHTML, wrapped in a <c>div</c> element.
        /// </summary>
        [XmlEnum("xhtml")]
        XHTML
    }
}