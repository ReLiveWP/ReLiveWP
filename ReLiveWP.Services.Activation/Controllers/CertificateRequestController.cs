using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReLiveWP.Services.Activation.Certificates;

namespace ReLiveWP.Services.Activation
{
    [ApiController]
    [Route("dcs/certificaterequest")]
    public class CertificateRequestController : ControllerBase
    {
        private readonly ILogger<CertificateRequestController> _logger;
        private readonly ICertificateService _certificateService;

        public CertificateRequestController(
            ILogger<CertificateRequestController> logger,
            ICertificateService certificateService7)
        {
            _logger = logger;
            _certificateService = certificateService7;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var cert = _certificateService.GetOrGenerateRootCACert();
            var bytes = cert.Export(X509ContentType.Pkcs12);
            return File(bytes, "application/pkcs12", "RootCertificate.pfx");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var requestCert = await (new StreamReader(Request.Body)).ReadToEndAsync();

            var data = _certificateService.HandleCertRequest(requestCert);
            var base64 = Convert.ToBase64String(data);
            return Content(base64, "application/c-x509-ca-cert");
        }       
    }
}
