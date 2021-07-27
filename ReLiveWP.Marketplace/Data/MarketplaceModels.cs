using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ReLiveWP.Marketplace.Data
{
    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class SignInResponse
    {
        public AccountState AccountState { get; set; } = new AccountState();
        public AccountInfo AccountInfo { get; set; } = new AccountInfo();
        public SubscriptionInfo SubscriptionInfo { get; set; } = new SubscriptionInfo();

        public List<MediaTypeTunerRegisterInfo> TunerRegisterInfo = new List<MediaTypeTunerRegisterInfo>()
        {
            new MediaTypeTunerRegisterInfo() { RegisterType = MediaType.AppStore, Activated = true },
            new MediaTypeTunerRegisterInfo() { RegisterType = MediaType.Subscription, Activated = false, Activable = false }
        };
    }

    public enum MediaType
    {
        Subscription,
        AppStore
    }

    public class MediaTypeTunerRegisterInfo
    {
        public MediaType RegisterType { get; set; }
        public bool Activated { get; set; }
        public bool Activable { get; set; }
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class AccountState
    {
        public uint SignInErrorCode { get; set; } = 0;
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class AccountInfo
    {
        public string ZuneTag { get; set; }
        public ulong Xuid { get; set; }
        public Guid UserReadID { get; set; }
        public Guid UserWriteID { get; set; }
        public string Locale { get; set; }
        public bool ParentallyControlled { get; set; } = false;
        public bool ExplicitPrivilege { get; set; } = false;
        public bool UsageCollectionAllowed { get; set; } = true;
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class SubscriptionInfo
    {
        public bool SubscriptionEnabled { get; set; } = false;
        public string SubscriptionMeteringCertificate { get; set; } = "";
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class BalanceResponse
    {
        public Balances Balances = new Balances();
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class Balances
    {
        public int SongCreditBalance = 1000;
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class AcquisitionInfoResponse
    {
        public List<AppAcquisitionInfoOutput> AppAcquisitionInfoOutputs { get; set; }
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class AppAcquisitionInfoOutput
    {
        public Guid AppId { get; set; }
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class GetCreditCardResponse { }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class PurchaseResponse { }
    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class LocationResponse { }
}
