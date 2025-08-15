using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

//
// so i'm no crypto expert, and a lot of this probably has some holes so like if you think you can fix it
// do contribute or at least let me know, but also keep in mind we're targeting pretty old devices, which
// don't always support the latest encryption standards.
//
// tl;dr i know SHA1 is insecure these days please do not email me about it
//

namespace ReLiveWP.Backend.DeviceRegistration.Certificates;

public class WP7CertificateService(
    ILogger<WP7CertificateService> logger,
    RootCACertificateProvider caProvider) : ICertificateService
{
    public byte[] HandleCertRequest(byte[] certificateRequest)
    {
        X509Certificate2 deviceCert;

        var rootCert = caProvider.GetOrGenerateRootCACert(true);
        var bcRootCert = DotNetUtilities.FromX509Certificate(rootCert);

        var random = SecureRandom.GetInstance("SHA_1PRNG");
        var certRequest = new Pkcs10CertificationRequest(certificateRequest);
        var certRequestInfo = certRequest.GetCertificationRequestInfo();

        using var localStore = new X509Store("Trusted Windows Phone devices", StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        var storeCollection = localStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certRequestInfo.Subject.ToString(), true);
        if (storeCollection.Count > 0)
        {
            logger.LogDebug("Found existing certificate for {SubjectDN}", certRequestInfo.Subject);
            deviceCert = storeCollection[0];

            storeCollection.Remove(deviceCert); // we need to reissue every time
        }

        logger.LogDebug("Generating new certificate for {SubjectDN}", certRequestInfo.Subject);
        var caKeyPair = DotNetUtilities.GetRsaKeyPair(rootCert.GetRSAPrivateKey());
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);

        // var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
        var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", caKeyPair.Private, random); // secure lol
        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.SetSerialNumber(serialNumber);
        certificateGenerator.SetIssuerDN(bcRootCert.SubjectDN);
        certificateGenerator.SetSubjectDN(certRequestInfo.Subject);
        certificateGenerator.SetNotBefore(DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)));
        certificateGenerator.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certificateGenerator.SetPublicKey(certRequest.GetPublicKey());

        var extendedUsage = new ExtendedKeyUsage(new List<object>()
        {
            KeyPurposeID.IdKPServerAuth,
            KeyPurposeID.IdKPClientAuth,
            new DerObjectIdentifier("1.3.6.1.4.1.311.71.1.1"), // WP7 key usage
            new DerObjectIdentifier("1.3.6.1.4.1.311.71.1.2"), // WP8 key usage (seems to be used by the marketplace)
            new DerObjectIdentifier("1.3.6.1.4.1.311.71.1.6"), // Windows Live key usage? Potentially PlayReady
            new DerObjectIdentifier("1.3.6.1.4.1.311.10.3.3"), // Microsoft allow export
            new DerObjectIdentifier("2.16.840.1.113730.4.1"),  // Netscape allow export
        });

        certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation | KeyUsage.KeyEncipherment | KeyUsage.DataEncipherment));
        certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, extendedUsage);

        foreach (var attribute in certRequestInfo.Attributes)
        {
            var attr = AttributePkcs.GetInstance(attribute);
            if (attr.AttrType == PkcsObjectIdentifiers.Pkcs9AtExtensionRequest)
            {
                var extensions = X509Extensions.GetInstance(attr.AttrValues[0]);
                foreach (var oid in extensions.ExtensionOids)
                {
                    var ext = extensions.GetExtension((DerObjectIdentifier)oid);
                    certificateGenerator.AddExtension((DerObjectIdentifier)oid, ext.IsCritical, ext.GetParsedValue());
                }
            }
        }

        var cert = certificateGenerator.Generate(signatureFactory);
        deviceCert = new X509Certificate2(cert.GetEncoded(), (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        localStore.Add(deviceCert);

        logger.LogInformation("Generated new certificate for {SubjectDN}", certRequestInfo.Subject);

        var chain = new X509Chain();
        chain.Build(deviceCert);

        var deviceStore = new X509Certificate2Collection();
        foreach (var item in chain.ChainElements)
            deviceStore.Add(item.Certificate);

        logger.LogInformation("Exported {Count} certificates into chain.", deviceStore.Count);
        return deviceStore.Export(X509ContentType.Pkcs7)!;
    }
}
