using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Tls;
using ReLiveWP.Backend.Certificates;

namespace ReLiveWP.Backend.ClientProvisioning
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
