using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class Balances
{
    public double PointsBalance { get; set; } = 1000;
    public double SongCreditBalance { get; set; }
    public string SongCreditRenewalDate { get; set; }
}
