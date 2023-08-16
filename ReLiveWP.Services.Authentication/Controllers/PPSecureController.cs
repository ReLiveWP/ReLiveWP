using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Security;
using ReLiveWP.Backend.Certificates;
using ReLiveWP.Backend.ClientProvisioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ReLiveWP.Services.Authentication.Controllers;

[Route("/ppsecure/{action}.srf")]
public class PPSecureController : Controller
{
    private const string JSPublicKeyFormat = " var Key=\"e={0};m={1}\"; var randomNum=\"{2}\"; var SKI=\"{3}\";";

    private RootCACertificateProvider _provider;
    private ClientProvisioning.ClientProvisioningClient _keyPairProvider;

    public PPSecureController(RootCACertificateProvider provider,
        ClientProvisioning.ClientProvisioningClient keyPairProvider)
    {
        _provider = provider;
        _keyPairProvider = keyPairProvider;
    }

    public async Task<IActionResult> JSPublicKey()
    {
        var certificate = _provider.GetOrGenerateRootCACert(false);
        var rsa = certificate.GetRSAPublicKey();
        var rsaParameters = rsa.ExportParameters(false);

        var random = SecureRandom.GetInstance("SHA_1PRNG");
        var buffer = new byte[100];
        random.NextBytes(buffer);

        var exponent = Convert.ToHexString(rsaParameters.Exponent!).ToLowerInvariant();
        var modulus = Convert.ToHexString(rsaParameters.Modulus!).ToLowerInvariant();
        var randomNum = Convert.ToHexString(buffer).ToLowerInvariant();

        return File(Encoding.UTF8.GetBytes(string.Format(JSPublicKeyFormat, exponent, modulus, randomNum, certificate.Thumbprint)), "application/x-javascript");
    }
}
