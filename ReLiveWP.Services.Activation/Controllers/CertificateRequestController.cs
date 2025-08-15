using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Pkcs;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activation.Controllers;

[ApiController]
[Route("dcs/certificaterequest")]
public class CertificateRequestController(
    ILogger<CertificateRequestController> logger,
    ClientProvisioning.ClientProvisioningClient clientProvisioning,
    DeviceRegistration.DeviceRegistrationClient deviceRegistration) : ControllerBase
{
    private const string ActivationProtocolVersionHeader = "X-Windows-Phone-Activation-Protocol-Version";
    private const string ActivationCodeHeader = "X-Windows-Phone-Activation-Code";
    private const string DeviceInfoHeader = "X-Windows-Phone-Device-Info";

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cert = await clientProvisioning.GetCACertificateAsync(new Empty());
        var bytes = cert.Certificate.ToByteArray();
        return File(bytes, "application/pkcs12", "RootCertificate.pfx");
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var headers = Request.Headers;
        if (!headers.TryGetValue(ActivationProtocolVersionHeader, out var protocolVersionHeader))
            return BadRequest("No Protocol Version header specified.");
        if (!headers.TryGetValue(ActivationCodeHeader, out var activationCodeHeader))
            return BadRequest("No activation code header specified.");
        if (!headers.TryGetValue(DeviceInfoHeader, out var deviceInfoHeader))
            return BadRequest("No device info header specified.");

        var version = protocolVersionHeader[0];
        if (version != "1.0") // TODO: WP8 support
            return BadRequest("Unsupported protocol version.");

        var activationCode = activationCodeHeader[0];
        var deviceInfo = deviceInfoHeader[0].Split(',')
                                            .Select(s => s.Split(':'))
                                            .ToDictionary(k => k[0], v => v.ElementAtOrDefault(1));

        logger.LogInformation("Provided key {ProductKey}", activationCode);

        var requestCert = await new StreamReader(Request.Body).ReadToEndAsync();

        var encoded = Convert.FromBase64String(requestCert);
        var certRequest = new Pkcs10CertificationRequest(encoded);
        var certRequestInfo = certRequest.GetCertificationRequestInfo();

        var registrationRequest = new DeviceRegistrationRequest
        {
            CertificateSubject = certRequestInfo.Subject.ToString(),
            ActivationCode = activationCode,

            // if we dont have these we're probably shafted anyway lol
            UniqueId = deviceInfo["DeviceUniqueID"],
            OsVersion = deviceInfo["OSVersion"],
            Locale = deviceInfo["Locale"]
        };

        // i don't care about these ones too much
        if (deviceInfo.TryGetValue("Manafacturer", out var manufacturer))
            registrationRequest.DeviceManufacturer = manufacturer;
        if (deviceInfo.TryGetValue("Model", out var model))
            registrationRequest.DeviceModel = model;
        if (deviceInfo.TryGetValue("Operator", out var @operator))
            registrationRequest.DeviceOperator = @operator;
        if (deviceInfo.TryGetValue("IMEI", out var imei))
            registrationRequest.DeviceIMEI = imei;

        // not tracked:
        // IMSI
        // ComOperator

        var response = await deviceRegistration.RegisterDeviceAsync(registrationRequest);
        if (!response.Succeeded)
        {
            return Unauthorized();
        }

        var provisioningRequest = new WP7ProvisioningRequest() { CertificateRequest = ByteString.CopyFrom(encoded) };
        var provisioningResponse = await clientProvisioning.ProvisionWP7DeviceAsync(provisioningRequest);
        if (provisioningResponse.Succeeded)
        {
            var base64 = provisioningResponse.Certificate.ToBase64();
            return Content(base64, "application/c-x509-ca-cert");
        }

        return Unauthorized();
    }
}
