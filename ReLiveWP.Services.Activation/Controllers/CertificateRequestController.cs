using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Pkcs;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.DeviceRegistration;

namespace ReLiveWP.Services.Activation.Controllers;

[ApiController]
[Route("dcs/certificaterequest")]
[Route("/certificaterequest")]
public class CertificateRequestController(
    ILogger<CertificateRequestController> logger,
    ClientProvisioning.ClientProvisioningClient clientProvisioning,
    DeviceRegistration.DeviceRegistrationClient deviceRegistration) : ControllerBase
{
    private const string ActivationProtocolVersionHeader = "X-Windows-Phone-Activation-Protocol-Version";
    private const string ActivationCodeHeader = "X-Windows-Phone-Activation-Code";
    private const string DeviceInfoHeader = "X-Windows-Phone-Device-Info";

    [HttpPost]
    [Consumes("application/pkcs10")]
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
        if (version != "1.0" && version != "2.0")
            return BadRequest("Unsupported protocol version.");

        var activationCode = activationCodeHeader[0];
        var deviceInfo = ParseDeviceInfo(deviceInfoHeader[0] ?? "");
        if (!deviceInfo.TryGetValue("DeviceUniqueID", out var uniqueId) || string.IsNullOrEmpty(uniqueId))
            return BadRequest("Device info header has no DeviceUniqueID.");

        logger.LogInformation("Provided key {ProductKey}", activationCode);

        if (activationCode == "NOPVK-NOPVK-NOPVK-NOPVK-NOPVK")
            return StatusCode(409);

        var requestCert = await new StreamReader(Request.Body).ReadToEndAsync();

        byte[] encoded;
        Pkcs10CertificationRequest certRequest;
        try
        {
            encoded = Convert.FromBase64String(requestCert);
            certRequest = new Pkcs10CertificationRequest(encoded);
        }
        catch (Exception ex) when (ex is FormatException or IOException or ArgumentException or InvalidCastException)
        {
            logger.LogWarning(ex, "Rejecting certificate request: body is not a base64 PKCS#10 request");
            return BadRequest("Malformed certificate request.");
        }

        var certRequestInfo = certRequest.GetCertificationRequestInfo();

        var registrationRequest = new DeviceRegistrationRequest
        {
            CertificateSubject = certRequestInfo.Subject.ToString(),
            ActivationCode = activationCode,
            UniqueId = uniqueId,
            OsVersion = deviceInfo.GetValueOrDefault("OSVersion", ""),
            Locale = deviceInfo.GetValueOrDefault("Locale", ""),
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
            return Unauthorized();

        var provisioningRequest = new DeviceProvisioningRequest() { CertificateRequest = ByteString.CopyFrom(encoded), Version = version };
        var provisioningResponse = await clientProvisioning.ProvisionDeviceAsync(provisioningRequest);
        if (provisioningResponse.Succeeded)
        {
            var base64 = provisioningResponse.Certificate.ToBase64();
            return Content(base64, "application/c-x509-ca-cert");
        }

        return Unauthorized();
    }

    // "DeviceUniqueID:abc,OSVersion:7.10.7720,Locale:0409,..."
    private static Dictionary<string, string> ParseDeviceInfo(string header)
    {
        var info = new Dictionary<string, string>();
        foreach (var pair in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = pair.IndexOf(':');
            if (colon <= 0)
                continue;

            info[pair[..colon]] = pair[(colon + 1)..];
        }

        return info;
    }
}
