using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace ReLiveWP.Backend.Certificates
{
    public class RootCACertificateProvider
    {
        private ILogger<RootCACertificateProvider> _logger;
        private IConfiguration _configuration;

        public RootCACertificateProvider(
            ILogger<RootCACertificateProvider> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public X509Certificate2 GetOrGenerateRootCACert(bool includePrivateKey)
        {
            var caDistinguishedName = _configuration["CertificateGeneration:RootCA"];
            var caName = new X509Name(caDistinguishedName);

            _logger.LogDebug("Looking for certificate with CN \"{Name}\"", caDistinguishedName);

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var collection = store.Certificates.Find(X509FindType.FindByIssuerName, "Windows Phone PCA", true);

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
                var certBuilder = new X509V3CertificateGenerator();

                var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", keyPair.Private, random); // secure lol
                var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
                certBuilder.SetSerialNumber(serialNumber);
                certBuilder.SetSubjectDN(caName);
                certBuilder.SetIssuerDN(caName);
                certBuilder.SetNotBefore(startingDateTime);
                certBuilder.SetNotAfter(endDateTime);
                certBuilder.SetPublicKey(keyPair.Public);

                var extendedUsage = new ExtendedKeyUsage(new List<object>() { KeyPurposeID.AnyExtendedKeyUsage });
                certBuilder.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation | KeyUsage.KeyEncipherment | KeyUsage.DataEncipherment | KeyUsage.CrlSign | KeyUsage.KeyCertSign));
                certBuilder.AddExtension(X509Extensions.ExtendedKeyUsage, true, extendedUsage);

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
