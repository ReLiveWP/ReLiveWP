using System;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Pkcs;
using ReLiveWP.Services.Grpc;

const string ActivationProtocolVersionHeader = "X-Windows-Phone-Activation-Protocol-Version";
const string ActivationCodeHeader = "X-Windows-Phone-Activation-Code";
const string DeviceInfoHeader = "X-Windows-Phone-Device-Info";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcClient<DeviceRegistration.DeviceRegistrationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:DeviceRegistration"]));
builder.Services.AddGrpcClient<ClientProvisioning.ClientProvisioningClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ClientProvisioning"]));

var app = builder.Build();

app.UseRouting();

app.Map("/dcs/certificaterequest", async (
    HttpContext context,
    ILogger logger,
    ClientProvisioning.ClientProvisioningClient clientProvisioning,
    DeviceRegistration.DeviceRegistrationClient deviceRegistration) =>
{
    var headers = context.Request.Headers;
    if (!headers.TryGetValue(ActivationProtocolVersionHeader, out var protocolVersionHeader))
        return Results.BadRequest("No Protocol Version header specified.");
    if (!headers.TryGetValue(ActivationCodeHeader, out var activationCodeHeader))
        return Results.BadRequest("No activation code header specified.");
    if (!headers.TryGetValue(DeviceInfoHeader, out var deviceInfoHeader))
        return Results.BadRequest("No device info header specified.");

    var version = protocolVersionHeader[0];
    if (version != "1.0") // TODO: WP8 support
        return Results.BadRequest("Unsupported protocol version.");

    var activationCode = activationCodeHeader[0];
    var deviceInfo = deviceInfoHeader[0].Split(',')
                                        .Select(s => s.Split(':'))
                                        .ToDictionary(k => k[0], v => v.ElementAtOrDefault(1));

    logger.LogInformation("Provided key {ProductKey}", activationCode);

    if (activationCode == "NOPVK-NOPVK-NOPVK-NOPVK-NOPVK")
        return Results.StatusCode(409);

    var requestCert = await new StreamReader(context.Request.Body).ReadToEndAsync();

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
        return Results.Unauthorized();
    }

    var provisioningRequest = new WP7ProvisioningRequest() { CertificateRequest = ByteString.CopyFrom(encoded) };
    var provisioningResponse = await clientProvisioning.ProvisionWP7DeviceAsync(provisioningRequest);
    if (provisioningResponse.Succeeded)
    {
        var base64 = provisioningResponse.Certificate.ToBase64();
        return Results.Content(base64, "application/c-x509-ca-cert");
    }

    return Results.Unauthorized();
});

app.Run();