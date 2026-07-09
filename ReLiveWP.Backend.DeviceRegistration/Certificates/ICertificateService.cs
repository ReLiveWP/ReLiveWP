namespace ReLiveWP.Backend.DeviceRegistration.Certificates
{
    public interface ICertificateService
    {
        byte[] HandleCertRequest(byte[] certificateRequest, bool isVersion2);
    }
}