using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Backend.DeviceRegistration.Certificates;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.DeviceRegistration.Services
{
    public class ClientProvisioningService(
        ILogger<ClientProvisioningService> logger,
        ICertificateService wp7CertificateService,
        RootCACertificateProvider caProvider) : ClientProvisioning.ClientProvisioningBase
    {
        public override Task<CACertificateResponse> GetCACertificate(Empty request, ServerCallContext context)
        {
            var cert = caProvider.GetOrGenerateRootCACert(true);
            var data = cert.Export(X509ContentType.Pkcs12);

            return Task.FromResult(new CACertificateResponse() { Certificate = ByteString.CopyFrom(data) });
        }

        public override Task<DeviceProvisioningResponse> ProvisionWP7Device(WP7ProvisioningRequest request, ServerCallContext context)
        {
            try
            {
                var certificate = wp7CertificateService.HandleCertRequest(request.CertificateRequest.ToByteArray());
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(certificate) });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to provision device!");
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = false });
            }
        }
    }
}
