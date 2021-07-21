using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Services.Activation.Certificates
{
    public interface ICertificateService
    {
        byte[] HandleCertRequest(string certificateRequest);

        X509Certificate2 GetOrGenerateRootCACert();
    }
}