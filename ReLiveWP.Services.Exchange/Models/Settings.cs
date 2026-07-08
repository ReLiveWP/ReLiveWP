using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Settings = "Settings";
}

[XmlRoot("Settings", Namespace = Constants.Settings)]
public class SettingsRequest
{
    [XmlElement("DeviceInformation", Namespace = Constants.Settings)]
    public SettingsDeviceInformationRequest? DeviceInformation { get; set; }

    [XmlElement("UserInformation", Namespace = Constants.Settings)]
    public SettingsUserInformationRequest? UserInformation { get; set; }
}

public class SettingsDeviceInformationRequest
{
    [XmlElement("Set", Namespace = Constants.Settings)]
    public DeviceInformationSet? Set { get; set; }
}

// Fields the client sends in DeviceInformation/Set (all request-only per spec §2.2.1.18)
public class DeviceInformationSet
{
    [XmlElement("Model", Namespace = Constants.Settings)]
    public string? Model { get; set; }

    [XmlElement("IMEI", Namespace = Constants.Settings)]
    public string? IMEI { get; set; }

    [XmlElement("FriendlyName", Namespace = Constants.Settings)]
    public string? FriendlyName { get; set; }

    [XmlElement("OS", Namespace = Constants.Settings)]
    public string? OS { get; set; }

    [XmlElement("OSLanguage", Namespace = Constants.Settings)]
    public string? OSLanguage { get; set; }

    [XmlElement("PhoneNumber", Namespace = Constants.Settings)]
    public string? PhoneNumber { get; set; }

    [XmlElement("UserAgent", Namespace = Constants.Settings)]
    public string? UserAgent { get; set; }

    [XmlElement("EnableOutboundSMS", Namespace = Constants.Settings)]
    public int? EnableOutboundSMS { get; set; }
    public bool ShouldSerializeEnableOutboundSMS() => EnableOutboundSMS.HasValue;

    [XmlElement("MobileOperator", Namespace = Constants.Settings)]
    public string? MobileOperator { get; set; }
}

// UserInformation/Get is an empty element — its presence signals the client wants email addresses
public class SettingsUserInformationRequest
{
    [XmlElement("Get", Namespace = Constants.Settings)]
    public UserInformationRequestGet? Get { get; set; }
}

public class UserInformationRequestGet { }

[XmlRoot("Settings", Namespace = Constants.Settings)]
public class SettingsResponse
{
    [XmlElement("DeviceInformation", Namespace = Constants.Settings)]
    public SettingsDeviceInformationResponse? DeviceInformation { get; set; }

    [XmlElement("UserInformation", Namespace = Constants.Settings)]
    public SettingsUserInformationResponse? UserInformation { get; set; }
}

// DeviceInformation response mirrors request structure: status is inside <Set>
public class SettingsDeviceInformationResponse
{
    [XmlElement("Set", Namespace = Constants.Settings)]
    public SettingsStatusOnly? Set { get; set; }
}

// UserInformation: Status is a direct child, then Get contains the account list.
// In protocol 14.1+, EmailAddresses lives inside Account, not directly under Get.
public class SettingsUserInformationResponse
{
    [XmlElement("Status", Namespace = Constants.Settings)]
    public int Status { get; set; }

    [XmlElement("Get", Namespace = Constants.Settings)]
    public UserInformationResponseGet? Get { get; set; }
}

public class UserInformationResponseGet
{
    // 14.1+: email addresses are inside Accounts/Account/EmailAddresses
    [XmlElement("Accounts", Namespace = Constants.Settings)]
    public UserAccounts? Accounts { get; set; }
}

public class UserAccounts
{
    [XmlElement("Account", Namespace = Constants.Settings)]
    public List<UserAccount> Items { get; set; } = [];
}

public class UserAccount
{
    // Omitted for the primary account per spec; secondary accounts carry an ID
    [XmlElement("AccountId", Namespace = Constants.Settings)]
    public string? AccountId { get; set; }

    [XmlElement("AccountName", Namespace = Constants.Settings)]
    public string? AccountName { get; set; }

    [XmlElement("UserDisplayName", Namespace = Constants.Settings)]
    public string? UserDisplayName { get; set; }

    // 0 = sending enabled; present even when 0 so clients know they can send
    [XmlElement("SendDisabled", Namespace = Constants.Settings)]
    public int SendDisabled { get; set; }

    [XmlElement("EmailAddresses", Namespace = Constants.Settings)]
    public UserEmailAddresses? EmailAddresses { get; set; }
}

public class UserEmailAddresses
{
    [XmlElement("SMTPAddress", Namespace = Constants.Settings)]
    public List<string> SMTPAddresses { get; set; } = [];

    // Primary SMTP address — used by SendMail when no AccountId is provided
    [XmlElement("PrimarySmtpAddress", Namespace = Constants.Settings)]
    public string? PrimarySmtpAddress { get; set; }
}

// Generic status-only container used inside Set/Get wrappers
public class SettingsStatusOnly
{
    [XmlElement("Status", Namespace = Constants.Settings)]
    public int Status { get; set; }
}
