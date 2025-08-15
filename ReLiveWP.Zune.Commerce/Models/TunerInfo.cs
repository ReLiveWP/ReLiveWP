using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class TunerInfo
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Version { get; set; }
}
