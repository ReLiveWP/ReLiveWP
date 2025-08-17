using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models;

[XmlRoot(ElementName = "category", Namespace = Atom.Constants.ATOM_NAMESPACE)]
public class Category
{
    public Category() { Term = null!; }
    public Category(string term)
    {
        Term = term;
    }

    [XmlAttribute(AttributeName = "term")]
    public string Term { get; set; }
}
