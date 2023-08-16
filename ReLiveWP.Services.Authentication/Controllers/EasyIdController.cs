using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using ReLiveWP.Backend.Certificates;
using ReLiveWP.Backend.ClientProvisioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace ReLiveWP.Services.Authentication.Controllers;

[Route("/services/easyId/{action}")]
public class EasyIdController : Controller
{

    private RootCACertificateProvider _provider;
    private ClientProvisioning.ClientProvisioningClient _keyPairProvider;

    public EasyIdController(RootCACertificateProvider provider,
        ClientProvisioning.ClientProvisioningClient keyPairProvider)
    {
        _provider = provider;
        _keyPairProvider = keyPairProvider;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var reader = new StreamReader(Request.Body);
        var data = await reader.ReadToEndAsync();
        var document = new XmlDocument();
        document.LoadXml(data);

        var certificate = _provider.GetOrGenerateRootCACert(true);

        var nsManager = new XmlNamespaceManager(document.NameTable);
        nsManager.AddNamespace("p", "http://schemas.microsoft.com/Passport/User");

        var cipherValue = document.SelectSingleNode("//p:CipherValue", nsManager)?.InnerText;
        if (cipherValue == null)
            return BadRequest();

        var encrypted = Convert.FromBase64String(cipherValue);
        var rsaParams = certificate.GetRSAPrivateKey().ExportParameters(true);

        var eng = new OaepEncoding(new RsaEngine(), new Sha1Digest());
        eng.Init(false, DotNetUtilities.GetRsaKeyPair(certificate.GetRSAPrivateKey()).Private);
        encrypted = eng.ProcessBlock(encrypted, 0, encrypted.Length);
        var result = Encoding.UTF8.GetString(encrypted);

        var rsa = certificate.GetRSAPrivateKey();
        //encrypted = rsa.Encrypt(new byte[128], RSAEncryptionPadding.OaepSHA1);
        var decrypted = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA1);

        return Ok();
    }
}
