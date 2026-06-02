using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace ReLiveWP.Backend.Identity.Certificates;

public class LiveIdDeviceCertificateService(
    ILogger<LiveIdDeviceCertificateService> logger,
    RootCACertificateProvider caProvider)
{
    public byte[] HandleCertRequest(string puid, byte[] certificateRequest)
    {
        X509Certificate2 deviceCert;

        var rootCert = caProvider.GetCACert(true);
        if (rootCert == null)
            throw new Exception("No root certificate available.");

        var bcRootCert = DotNetUtilities.FromX509Certificate(rootCert);
        var certSubject = new X509Name($"CN={puid}.devicedns.live.com");

        var random = SecureRandom.GetInstance("SHA_1PRNG");
        var certRequest = new Pkcs10CertificationRequest(certificateRequest);
        var certRequestInfo = certRequest.GetCertificationRequestInfo();

        using var localStore = new X509Store("Trusted Windows Live devices", StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        var storeCollection = localStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certSubject.ToString(), true);
        if (storeCollection.Count > 0)
        {
            logger.LogDebug("Found existing certificate for {SubjectDN}", certSubject);
            deviceCert = storeCollection[0];

            storeCollection.Remove(deviceCert); // we need to reissue every time
        }

        logger.LogDebug("Generating new certificate for {SubjectDN}", certSubject);
        var caKeyPair = DotNetUtilities.GetRsaKeyPair(rootCert.GetRSAPrivateKey());
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);

        // var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
        var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", caKeyPair.Private, random); // secure lol
        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.SetSerialNumber(serialNumber);
        certificateGenerator.SetIssuerDN(bcRootCert.SubjectDN);
        certificateGenerator.SetSubjectDN(certSubject);
        certificateGenerator.SetNotBefore(DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)));
        certificateGenerator.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certificateGenerator.SetPublicKey(certRequest.GetPublicKey());

        var extendedUsage = new ExtendedKeyUsage(new List<object>()
        {
            KeyPurposeID.IdKPServerAuth,
            KeyPurposeID.IdKPClientAuth,
            new DerObjectIdentifier("1.3.6.1.4.1.311.10.3.3"), // Microsoft allow export
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

        logger.LogInformation("Generated new certificate for {SubjectDN}", certSubject);

        return deviceCert.Export(X509ContentType.Cert);
    }

    public bool ValidateDeviceCertificate(X509Certificate2 certificate)
    {
        var certChain = caProvider.GetCACertChain();

        using var chain = new X509Chain(false);
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidBasicConstraints;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        foreach (var cert in certChain)
            chain.ChainPolicy.CustomTrustStore.Add(cert);

        if (!chain.Build(certificate))
        {
            foreach (var item in chain.ChainStatus)
            {
                if (item.Status == 0)
                    continue;

                logger.LogWarning("Device certificate validation failed: {Status} {StatusInfo}", item.Status, item.StatusInformation);
            }

            return false;
        }

        return true;
    }
}
