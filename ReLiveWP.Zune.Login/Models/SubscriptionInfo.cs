using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class SubscriptionInfo
{
    public Guid? SubscriptionOfferId { get; set; }
    public Guid? SubscriptionRenewalOfferId { get; set; }
    public Guid? BillingInstanceId { get; set; }
    public bool SubscriptionEnabled { get; set; }
    public bool SubscriptionBillingViolation { get; set; }
    public bool SubscriptionPendingCancel { get; set; }
    public string SubscriptionStartDate { get; set; } = DateTime.Now.AddDays(-5).ToString();
    public string SubscriptionEndDate { get; set; } = DateTime.Now.AddDays(5).ToString();
    public string SubscriptionMeteringCertificate { get; set; }
    public string LastLabelTakedownDate { get; set; }
    public TunerRegisterInfo MediaTypeTunerRegisterInfo { get; set; }
}
