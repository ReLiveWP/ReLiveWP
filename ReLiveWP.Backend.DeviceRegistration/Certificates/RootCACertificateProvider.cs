using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Backend.DeviceRegistration.Certificates;

public class RootCACertificateProvider(
    ILogger<RootCACertificateProvider> logger,
    IConfiguration configuration)
{
    public X509Certificate2? GetOrGenerateRootCACert(bool includePrivateKey)
    {
        var caDistinguishedName = configuration["CertificateGeneration:RootCA"]?
            .Replace("ST=", "S=");

        if (!string.IsNullOrWhiteSpace(caDistinguishedName))
        {
            logger.LogDebug("Looking for certificate with CN \"{Name}\"", caDistinguishedName);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var collection = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, caDistinguishedName, false);

            if (collection.Count > 0)
            {
                var foundCert = collection[0];
                logger.LogDebug("Found certificate {Cert}", foundCert.Thumbprint);
                return includePrivateKey ? foundCert : new X509Certificate2(foundCert.RawData);
            }
            else
            {
                logger.LogWarning("No certificate found, this is bad!!!");

                return null;
            }
        }

        var caCertFile = configuration["CertificateGeneration:RootCACertFile"];
        var caCertFilePassword = configuration["CertificateGeneration:RootCACertPassword"];
        if (!string.IsNullOrWhiteSpace(caCertFile))
        {
            var certificate = new X509Certificate2(caCertFile, caCertFilePassword);
            return certificate;
        }

        return null;
    }
}
