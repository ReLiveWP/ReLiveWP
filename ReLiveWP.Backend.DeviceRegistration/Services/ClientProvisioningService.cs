using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using ReLiveWP.Backend.Certificates;

namespace ReLiveWP.Backend.ClientProvisioning
{
    public class ClientProvisioningService : ClientProvisioning.ClientProvisioningBase
    {
        private WP7CertificateService _wp7CertificateService;

        public ClientProvisioningService(WP7CertificateService wp7CertificateService)
        {
            _wp7CertificateService = wp7CertificateService;
        }

        public override Task<DeviceProvisioningResponse> ProvisionWP7Device(WP7ProvisioningRequest request, ServerCallContext context)
        {
            try
            {
                var certificate = _wp7CertificateService.HandleCertRequest(request.CertificateRequest.ToByteArray());
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(certificate) });
            }
            catch 
            {
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = false });
            }
        }
    }
}
