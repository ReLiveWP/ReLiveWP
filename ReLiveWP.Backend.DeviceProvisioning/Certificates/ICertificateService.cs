using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Backend.Certificates
{
    public interface ICertificateService
    {
        byte[] HandleCertRequest(byte[] certificateRequest);
    }
}