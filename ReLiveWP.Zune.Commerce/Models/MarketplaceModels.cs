using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class AcquisitionInfoResponse
{
    public List<AppAcquisitionInfoOutput> AppAcquisitionInfoOutputs { get; set; }
}

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class AppAcquisitionInfoOutput
{
    public Guid AppId { get; set; }
}

[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class GetCreditCardResponse { }
[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class PurchaseResponse { }
[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class LocationResponse
{
    public List<LocationOutput> LocationOutputs { get; set; } = new List<LocationOutput>();
}

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class LocationOutput
{
    public Guid KeyID { get; set; }

    [XmlElement(ElementName = "downloadUrl")]
    public string DownloadUrl { get; set; }

    [XmlElement(ElementName = "downloadAcknowledgementUrl")]
    public string DownloadAcknowledgementUrl { get; set; }
}

// LicenceResponse
[XmlRoot(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class LicenseResponse
{
    public List<LicenseOutput> LicenseOutputs { get; set; } = new List<LicenseOutput>();
}

// 1001ec58	"GemXmlParser_ReadStringValueFromStream( pReceiveStream, L\"LicenseResponse/LicenseOutputs/LicenseOutput/License\", &strRawLicenseReponse )"	
// 10020b80	"GemXmlParser_ReadStringValueFromStream( READXMLSTRING_FLAG_BAD_XML_OK | READXMLSTRING_FLAG_MISSING_VALUE_OK, pStream, L\"LicenseResponse/LicenseOutputs/LicenseOutput/Error/ErrorCode\", &strErrorCode, &dwResult)"	

[XmlType(Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class LicenseOutput
{
    public Guid KeyID { get; set; }
    public string License { get; set; } // base64 encoded PlayReady license
}
