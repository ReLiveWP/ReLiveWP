using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
            var caDistinguishedName = _configuration["CertificateGeneration:RootCA"]
                .Replace("ST=", "S=");

            _logger.LogDebug("Looking for certificate with CN \"{Name}\"", caDistinguishedName);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var collection = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, caDistinguishedName, false);

            if (collection.Count > 0)
            {
                var foundCert = collection[0];
                _logger.LogDebug("Found certificate {Cert}", foundCert.Thumbprint);
                return includePrivateKey ? foundCert : new X509Certificate2(foundCert.RawData);
            }
            else
            {
                _logger.LogWarning("No certificate found, this is bad!!!");

                return null;
            }
        }
    }
}
