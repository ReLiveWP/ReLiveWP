using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class AccountState
{
    public int SignInErrorCode { get; set; } = 0;
    public bool TagChangeRequired { get; set; } = false;
    public bool AcceptedTermsOfService { get; set; } = true;
    public bool AccountSuspended { get; set; } = false;
    public bool SubscriptionLapsed { get; set; } = false;
    public bool BillingUnavailable { get; set; } = false;
}
