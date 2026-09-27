using System.Xml.Serialization;

namespace ReLiveWP.Services.AddressBook.Models;

[XmlRoot("DeviceSubscriptionInfoList")]
public class DeviceSubscriptionInfoList
{
    [XmlArray("Subscriptions")]
    [XmlArrayItem("DeviceSubscriptionInfo")]
    public List<DeviceSubscriptionInfo> Subscriptions { get; set; } = [];
}

public class DeviceSubscriptionInfo
{
    [XmlElement("View")]
    public string? View { get; set; }

    [XmlElement("RequestedExpirationPeriodInSeconds")]
    public int RequestedExpirationPeriodInSeconds { get; set; }

    [XmlElement("SubscriptionDetails")]
    public SubscriptionDetails? SubscriptionDetails { get; set; }
}

[XmlInclude(typeof(ContactSubscriptionDetails))]
public abstract class SubscriptionDetails { }

// wire shape nests <ContactSubscriptionDetail> twice before the payload
public class ContactSubscriptionDetails : SubscriptionDetails
{
    [XmlElement("ContactSubscriptionDetail")]
    public ContactSubscriptionDetailWrapper? Detail { get; set; }
}

public class ContactSubscriptionDetailWrapper
{
    [XmlElement("ContactSubscriptionDetail")]
    public ContactSubscriptionDetail? Detail { get; set; }
}

public class ContactSubscriptionDetail
{
    [XmlElement("ChannelURI")]
    public string? ChannelUri { get; set; }

    [XmlArray("ContactHandles")]
    [XmlArrayItem("ContactHandle")]
    public List<ContactHandle> ContactHandles { get; set; } = [];
}

public class ContactHandle
{
    [XmlElement("SourceHandle")]
    public SourceHandle? SourceHandle { get; set; }
}

public class SourceHandle
{
    [XmlElement("SourceID")]
    public string? SourceId { get; set; }

    [XmlElement("ObjectID")]
    public string? ObjectId { get; set; }
}
