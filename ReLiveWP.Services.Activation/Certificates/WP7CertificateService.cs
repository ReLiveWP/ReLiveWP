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

//
// so i'm no crypto expert, and a lot of this probably has some holes so like if you think you can fix it
// do contribute or at least let me know, but also keep in mind we're targeting pretty old devices, which
// don't always support the latest encryption standards.
//
// tl;dr i know SHA1 is insecure these days please do not email me about it
//

namespace ReLiveWP.Services.Activation.Certificates
{
    public class WP7CertificateService : ICertificateService
    {
        private ILogger<WP7CertificateService> _logger;
        private IConfiguration _configuration;

        public WP7CertificateService(
            ILogger<WP7CertificateService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public X509Certificate2 GetOrGenerateRootCACert()
        {
            return GetOrGenerateRootCACert(false); // avoid exposing private keys lmao
        }

        public byte[] HandleCertRequest(string certificateRequest)
        {
            var rootCert2 = GetOrGenerateRootCACert(true);
            var rootCert = DotNetUtilities.FromX509Certificate(rootCert2);

            var random = SecureRandom.GetInstance("SHA_1PRNG");
            var certRequest = new Pkcs10CertificationRequest(Convert.FromBase64String(certificateRequest));
            var certRequestInfo = certRequest.GetCertificationRequestInfo();

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
                new DerObjectIdentifier("1.3.6.1.4.1.311.71.1.1") // WP7 key usage
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

            //var certs = new List<object> { cert };
            //var store = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(certs));

            //var gen = new CmsSignedDataGenerator();
            //gen.AddSigner(caKeyPair.Private, bcCert, CmsSignedGenerator.DigestSha1);
            //gen.AddCertificates(store);

            var collection = new X509Certificate2Collection(new X509Certificate2(cert.GetEncoded()));
            var data = collection.Export(X509ContentType.Pkcs7);

            return data;
        }

        private X509Certificate2 GetOrGenerateRootCACert(bool includePrivateKey)
        {
            var caDistinguishedName = _configuration["CertificateGeneration:RootCA"];
            var caCommonName = _configuration["CertificateGeneration:RootCACN"];
            var caName = new X509Name(caDistinguishedName);

            _logger.LogDebug("Looking for certificate with CN \"{Name}\"", caCommonName);

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var collection = store.Certificates.Find(X509FindType.FindBySubjectName, caCommonName, true);

            if (collection.Count > 0)
            {
                var foundCert = collection[0];
                _logger.LogDebug("Found certificate {Cert}", foundCert.Thumbprint);
                return includePrivateKey ? foundCert : new X509Certificate2(foundCert.RawData);
            }
            else
            {
                _logger.LogWarning("No certificate found, creating new! Make sure your device trusts it!");

                var random = SecureRandom.GetInstance("SHA_1PRNG");
                var rsa = new RsaKeyPairGenerator();
                rsa.Init(new KeyGenerationParameters(random, 4096));

                var keyPair = rsa.GenerateKeyPair();
                var startingDateTime = new DateTime(2010, 1, 1);
                var endDateTime = new DateTime(2110, 1, 1);

                // var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
                var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
                var certBuilder = new X509V1CertificateGenerator();

                var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", keyPair.Private, random); // secure lol
                var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
                certBuilder.SetSerialNumber(serialNumber);
                certBuilder.SetSubjectDN(caName);
                certBuilder.SetIssuerDN(caName);
                certBuilder.SetNotBefore(startingDateTime);
                certBuilder.SetNotAfter(endDateTime);
                certBuilder.SetPublicKey(keyPair.Public);

                var cert = certBuilder.Generate(signatureFactory);

                var x509 = new X509Certificate2(cert.GetEncoded());
                var seq = (Asn1Sequence)Asn1Object.FromByteArray(privateKeyInfo.ParsePrivateKey().GetDerEncoded());
                var rsaKey = RsaPrivateKeyStructure.GetInstance(seq);
                var rsaParams = new RsaPrivateCrtKeyParameters(rsaKey);

                var x509WithKey = x509.CopyWithPrivateKey(DotNetUtilities.ToRSA(rsaParams));
                store.Add(x509WithKey);
                store.Close();

                _logger.LogInformation("Created and stored certificate {Cert}", x509.Thumbprint);
                return includePrivateKey ? x509WithKey : x509;
            }
        }
    }
}
