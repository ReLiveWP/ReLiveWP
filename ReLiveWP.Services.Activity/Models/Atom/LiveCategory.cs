using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlRoot(ElementName = "category", Namespace = Atom.Constants.Atom_Namespace)]
public class LiveCategory
{
    public LiveCategory() { Term = null!; }
    public LiveCategory(string term)
    {
        Term = term;
    }

    [XmlAttribute(AttributeName = "term")]
    public string Term { get; set; }
}
