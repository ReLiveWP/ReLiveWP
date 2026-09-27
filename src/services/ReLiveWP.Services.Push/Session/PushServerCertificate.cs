using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Services.Push.Session;

public sealed class PushServerCertificate(IConfiguration configuration) : IDisposable
{
    public X509Certificate2 Certificate { get; } = X509CertificateLoader.LoadPkcs12FromFile(
        configuration["Push:ServerCertPath"], configuration["Push:ServerCertPassword"]);

    public void Dispose() => Certificate.Dispose();
}
