using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Backend.Identity.Certificates;

public class RootCACertificateProvider(
    ILogger<RootCACertificateProvider> logger,
    IConfiguration configuration)
{
    public X509Certificate2? GetCACert(bool includePrivateKey)
    {
        var certificate2 = LookupCertificate(configuration["CertificateGeneration:CA"], includePrivateKey);
        if (certificate2 != null)
        {
            return certificate2;
        }

        var caCertFile = configuration["CertificateGeneration:CACertFile"];
        var caCertFilePassword = configuration["CertificateGeneration:CACertPassword"];
        if (!string.IsNullOrWhiteSpace(caCertFile))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(caCertFile, caCertFilePassword);
            return certificate;
        }

        return null;
    }
    public X509Certificate2? GetRootCACert(bool includePrivateKey)
    {
        var certificate2 = LookupCertificate(configuration["CertificateGeneration:RootCA"], includePrivateKey);
        if (certificate2 != null)
        {
            return certificate2;
        }

        var caCertFile = configuration["CertificateGeneration:RootCACertFile"];
        var caCertFilePassword = configuration["CertificateGeneration:RootCACertPassword"];
        if (!string.IsNullOrWhiteSpace(caCertFile))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(caCertFile, caCertFilePassword);
            return certificate;
        }

        return null;
    }

    public X509Certificate2Collection GetCACertChain()
    {
        var ca = GetCACert(false) 
            ?? throw new InvalidOperationException("No CA certificate!");
        var root = GetRootCACert(false)
             ?? throw new InvalidOperationException("No Root CA certificate!");

        return [.. new X509Certificate2[] { ca, root }];
    }

    private X509Certificate2? LookupCertificate(string? dn, bool includePrivateKey)
    {
        var caDistinguishedName = dn?.Replace("ST=", "S=");
        if (!string.IsNullOrWhiteSpace(caDistinguishedName))
        {
            logger.LogDebug("Looking for certificate with CN \"{Name}\"", caDistinguishedName);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var collection = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, caDistinguishedName, false);
            if (collection.Count > 0)
            {
                var foundCert = collection[0];
                logger.LogDebug("Found certificate {Cert}", foundCert.Thumbprint);
                return includePrivateKey ? foundCert : X509CertificateLoader.LoadCertificate(foundCert.RawData);
            }
            else
            {
                logger.LogWarning("No certificate found, this is bad!!!");
                return null;
            }
        }

        return null;
    }
}
