using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Math;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Org.BouncyCastle.Cms;

//
// so i'm no crypto expert, and a lot of this probably has some holes so like if you think you can fix it
// do contribute or at least let me know, but also keep in mind we're targeting pretty old devices, which
// don't always support the latest encryption standards.
//
// tl;dr i know SHA1 is insecure these days please do not email me about it
//

namespace ReLiveWP.Backend.Certificates
{
    public class WP7CertificateService : ICertificateService
    {
        private ILogger<WP7CertificateService> _logger;
        private RootCACertificateProvider _caProvider;

        public WP7CertificateService(
            ILogger<WP7CertificateService> logger,
            RootCACertificateProvider caProvider)
        {
            _logger = logger;
            _caProvider = caProvider;
        }

        public byte[] HandleCertRequest(byte[] certificateRequest)
        {
            var rootCert2 = _caProvider.GetOrGenerateRootCACert(true);
            var rootCert = DotNetUtilities.FromX509Certificate(rootCert2);

            var random = SecureRandom.GetInstance("SHA_1PRNG");
            var certRequest = new Pkcs10CertificationRequest(certificateRequest);
            var certRequestInfo = certRequest.GetCertificationRequestInfo();

            using var store = new X509Store("Trusted Windows Phone devices", StoreLocation.CurrentUser, OpenFlags.ReadWrite);
            //var storeCollection = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certRequestInfo.Subject.ToString(), true);
            //if (storeCollection.Count > 0)
            //{
            //    _logger.LogDebug("Found existing certificate for {SubjectDN}", certRequestInfo.Subject);

            //    var deviceCert = storeCollection[0];
            //    var deviceStore = new X509Certificate2Collection(new[] { deviceCert });
            //    return deviceStore.Export(X509ContentType.Pkcs7);
            //}

            _logger.LogDebug("Generating new certificate for {SubjectDN}", certRequestInfo.Subject);
            var caKeyPair = DotNetUtilities.GetRsaKeyPair(rootCert2.GetRSAPrivateKey());
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);

            // var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", caKeyPair.Private, random); // secure lol
            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetIssuerDN(rootCert.SubjectDN);
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
                new DerObjectIdentifier("1.3.6.1.4.1.311.71.1.6")  // Windows Live key usage? Potentially PlayReady
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
            var certificate = new X509Certificate2(cert.GetEncoded());
            store.Add(certificate);
            store.Close();

            var collection = new X509Certificate2Collection();
            collection.Add(certificate);
            collection.Add(rootCert2);

            var data = collection.Export(X509ContentType.Pkcs7);

            _logger.LogInformation("Generated new certificate for {SubjectDN}", certRequestInfo.Subject);

            return data;
        }
    }
}
