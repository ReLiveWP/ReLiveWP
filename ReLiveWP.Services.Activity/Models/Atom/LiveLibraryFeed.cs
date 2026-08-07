using System.Xml.Serialization;
using Atom.Attributes;
using Atom.Xml;

namespace ReLiveWP.Services.Activity.Models.Atom;

[NamespacePrefix("live", Constants.Live_Namespace)]
[NamespacePrefix("a", Constants.Atom_Namespace)]
[XmlInclude(typeof(LiveLibraryEntry))]
[XmlRoot(ElementName = "feed", Namespace = Constants.Atom_Namespace)]
public class LiveLibraryFeed : Feed<LiveLibraryEntry>
{
    [XmlElement(ElementName = "quotaUsed", Namespace = Constants.Live_Namespace)]
    public long? QuotaUsed { get; set; }

    public bool ShouldSerializeQuotaUsed() => QuotaUsed != null;

    [XmlElement(ElementName = "totalQuota", Namespace = Constants.Live_Namespace)]
    public long? TotalQuota { get; set; }
    public bool ShouldSerializeTotalQuota() => TotalQuota != null;

    [XmlElement(ElementName = "maxFileSize", Namespace = Constants.Live_Namespace)]
    public long? MaxFileSize { get; set; }
    public bool ShouldSerializeMaxFileSize() => MaxFileSize != null;
}
