using System.Collections.Generic;
using System.Xml.Serialization;

namespace Atom.Xml
{
    [XmlRoot("feed", Namespace = Constants.ATOM_NAMESPACE)]
    public class Feed : Feed<Entry>
    {
    }

    [XmlRoot("feed", Namespace = Constants.ATOM_NAMESPACE)]
    public class Feed<T> : Root where T : class
    {
        [XmlElement(ElementName = "entry")]
        public List<T> Entries { get; set; } = new List<T>();
    }
}
