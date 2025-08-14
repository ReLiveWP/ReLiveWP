using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class BalanceResponse
{
    public Balances Balances = new Balances();
}
