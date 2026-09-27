using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Backend.DeviceRegistration.Certificates;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.DeviceRegistration.Services;

public class ClientProvisioningService(
    ILogger<ClientProvisioningService> logger,
    ICertificateService wp7CertificateService) : ClientProvisioning.ClientProvisioningBase
{
    public override Task<DeviceProvisioningResponse> ProvisionDevice(DeviceProvisioningRequest request, ServerCallContext context)
    {
        try
        {
            var certificate = wp7CertificateService.HandleCertRequest(request.CertificateRequest.ToByteArray(), request.Version == "2.0");
            return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(certificate) });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision device!");
            return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = false });
        }
    }
}
