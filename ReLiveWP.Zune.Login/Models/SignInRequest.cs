using System.Xml.Serialization;

namespace ReLiveWP.Zune.Commerce.Models;

[XmlRoot(ElementName = nameof(SignInRequest), Namespace = "http://schemas.zune.net/commerce/2009/01")]
public class SignInRequest
{
    public TunerInfo TunerInfo { get; set; }
}
