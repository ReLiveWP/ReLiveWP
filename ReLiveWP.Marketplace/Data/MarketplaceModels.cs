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

        public List<TunerRegisterInfo> TunerRegisterInfo = new List<TunerRegisterInfo>()
        {
            new TypeTunerRegisterInfo() { RegisterType = TunerRegisterType.AppStore, Activated = true },
            new TypeTunerRegisterInfo() { RegisterType = TunerRegisterType.Subscription, Activated = false, Activable = false }
        };
    }

    public class TunerRegisterInfo
    {
    }

    public enum TunerRegisterType
    {
        Subscription,
        AppStore
    }

    public class TypeTunerRegisterInfo : TunerRegisterInfo
    {
        public TunerRegisterType RegisterType { get; set; }
        public bool Activated { get; set; }
        public bool Activable { get; set; }
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class AccountState
    {
        public int SignInErrorCode { get; set; } = 0;
        public bool TagChangeRequired { get; set; } = false;
        public bool AcceptedTermsOfService { get; set; } = true;
        public bool AccountSuspended { get; set; } = false;
        public bool SubscriptionLapsed { get; set; } = false;
        public bool BillingUnavailable { get; set; } = true;
    }

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

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class SubscriptionInfo
    {
        public Guid SubscriptionOfferId { get; set; }
        public Guid SubscriptionRenewalOfferId { get; set; }
        public Guid BillingInstanceId { get; set; }
        public bool SubscriptionEnabled { get; set; }
        public bool SubscriptionBillingViolation { get; set; }
        public bool SubscriptionPendingCancel { get; set; }
        public string SubscriptionStartDate { get; set; }
        public string SubscriptionEndDate { get; set; }
        public string SubscriptionMeteringCertificate { get; set; }
        public string LastLabelTakedownDate { get; set; }
        public TunerRegisterInfo MediaTypeTunerRegisterInfo { get; set; }
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class BalanceResponse
    {
        public Balances Balances = new Balances();
    }

    [XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
    public class Balances
    {
        public double PointsBalance { get; set; }
        public double SongCreditBalance { get; set; }
        public string SongCreditRenewalDate { get; set; }
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
