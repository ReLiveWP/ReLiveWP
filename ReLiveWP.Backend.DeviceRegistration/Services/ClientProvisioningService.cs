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
    public class ClientProvisioningService : ClientProvisioning.ClientProvisioningBase
    {
        private ILogger<ClientProvisioningService> _logger;
        private WP7CertificateService _wp7CertificateService;
        private RootCACertificateProvider _caProvider;

        public ClientProvisioningService(
            ILogger<ClientProvisioningService> logger,
            WP7CertificateService wp7CertificateService,
            RootCACertificateProvider caProvider)
        {
            _logger = logger;
            _caProvider = caProvider;
            _wp7CertificateService = wp7CertificateService;
        }

        public override Task<CACertificateResponse> GetCACertificate(Empty request, ServerCallContext context)
        {
            var cert = _caProvider.GetOrGenerateRootCACert(true);
            var data = cert.Export(X509ContentType.Pkcs12);

            return Task.FromResult(new CACertificateResponse() { Certificate = ByteString.CopyFrom(data) });
        }

        public override Task<DeviceProvisioningResponse> ProvisionWP7Device(WP7ProvisioningRequest request, ServerCallContext context)
        {
            try
            {
                var certificate = _wp7CertificateService.HandleCertRequest(request.CertificateRequest.ToByteArray());
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(certificate) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to provision device!");
                return Task.FromResult(new DeviceProvisioningResponse() { Succeeded = false });
            }
        }
    }
}
