using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class SignInResponse
{
    public AccountState AccountState { get; set; } = new AccountState();
    public AccountInfo AccountInfo { get; set; } = new AccountInfo();
    public Balances Balances { get; set; } = new Balances();
    public SubscriptionInfo SubscriptionInfo { get; set; } = new SubscriptionInfo();

    [XmlArrayItem(Type = typeof(MediaTypeTunerRegisterInfo))]
    public List<TunerRegisterInfo> TunerRegisterInfo = new List<TunerRegisterInfo>()
    {
        new MediaTypeTunerRegisterInfo() { RegisterType = TunerRegisterType.AppStore, Activated = true },
        new MediaTypeTunerRegisterInfo() { RegisterType = TunerRegisterType.Subscription, Activated = true, Activable = true }
    };
}
