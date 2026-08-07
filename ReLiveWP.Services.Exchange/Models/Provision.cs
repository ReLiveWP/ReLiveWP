using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Provision = "Provision";
}

[XmlRoot("Provision", Namespace = Constants.Provision)]
public class ProvisionRequest
{
    [XmlElement("DeviceInformation", Namespace = Constants.Settings)]
    public SettingsDeviceInformationRequest? DeviceInformation { get; set; }

    [XmlElement("Policies", Namespace = Constants.Provision)]
    public ProvisionPolicies? Policies { get; set; }
}

public class ProvisionPolicies
{
    [XmlElement("Policy", Namespace = Constants.Provision)]
    public ProvisionPolicy? Policy { get; set; }
}

// one class covers request and both response phases; ShouldSerialize* trims
// members that don't belong in a given message
public class ProvisionPolicy
{
    [XmlElement("PolicyType", Namespace = Constants.Provision)]
    public string PolicyType { get; set; } = ProvisionResponse.PolicyTypeWbxml;

    [XmlElement("PolicyKey", Namespace = Constants.Provision)]
    public string? PolicyKey { get; set; }
    public bool ShouldSerializePolicyKey() => PolicyKey is not null;

    [XmlElement("Status", Namespace = Constants.Provision)]
    public string? Status { get; set; }
    public bool ShouldSerializeStatus() => Status is not null;

    [XmlElement("Data", Namespace = Constants.Provision)]
    public ProvisionData? Data { get; set; }
    public bool ShouldSerializeData() => Data is not null;
}

public class ProvisionData
{
    [XmlElement("EASProvisionDoc", Namespace = Constants.Provision)]
    public EasProvisionDoc? EasProvisionDoc { get; set; }
}

[XmlRoot("Provision", Namespace = Constants.Provision)]
public class ProvisionResponse
{
    public const string PolicyTypeWbxml = "MS-EAS-Provisioning-WBXML";

    // distinct non-zero constants; nothing validates them, no enforcement gate
    public const string TempPolicyKey = "1307199584";
    public const string FinalPolicyKey = "3942919513";

    [XmlElement("Status", Namespace = Constants.Provision)]
    public string Status { get; set; } = "1";

    [XmlElement("DeviceInformation", Namespace = Constants.Settings)]
    public ProvisionDeviceInformationResponse? DeviceInformation { get; set; }

    [XmlElement("Policies", Namespace = Constants.Provision)]
    public ProvisionPolicies Policies { get; set; } = new();
}

public class ProvisionDeviceInformationResponse
{
    [XmlElement("Status", Namespace = Constants.Settings)]
    public int Status { get; set; } = 1;
}

// permissive no-op policy: only DevicePasswordEnabled=0 (no forced lock PIN) matters,
// everything else is left unrestrictive
public class EasProvisionDoc
{
    [XmlElement("DevicePasswordEnabled", Namespace = Constants.Provision)]
    public int DevicePasswordEnabled { get; set; } = 0;

    [XmlElement("AlphanumericDevicePasswordRequired", Namespace = Constants.Provision)]
    public int AlphanumericDevicePasswordRequired { get; set; } = 0;

    [XmlElement("PasswordRecoveryEnabled", Namespace = Constants.Provision)]
    public int PasswordRecoveryEnabled { get; set; } = 0;

    [XmlElement("RequireStorageCardEncryption", Namespace = Constants.Provision)]
    public int RequireStorageCardEncryption { get; set; } = 0;

    [XmlElement("AttachmentsEnabled", Namespace = Constants.Provision)]
    public int AttachmentsEnabled { get; set; } = 1;

    [XmlElement("RequireDeviceEncryption", Namespace = Constants.Provision)]
    public int RequireDeviceEncryption { get; set; } = 0;
}
