using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class AccountInfo
{
    public string ZuneTag { get; set; }
    public ulong Xuid { get; set; }
    public Guid UserReadID { get; set; }
    public Guid UserWriteID { get; set; }
    public string Locale { get; set; }
    public bool Lightweight { get; set; } = false;
    public bool ParentallyControlled { get; set; } = false;
    public bool ExplicitPrivilege { get; set; } = false;
    public bool UsageCollectionAllowed { get; set; } = true;
}
