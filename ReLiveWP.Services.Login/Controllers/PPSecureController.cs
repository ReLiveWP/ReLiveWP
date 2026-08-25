using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Razor.Templating.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using ReLiveWP.Services.Login.Models.DeviceCredential;
using ReLiveWP.Services.Login.Models.Ppsecure;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Controllers;

[Consumes("application/soap+xml")]
[Produces("application/soap+xml")]
[Route("/ppsecure/[action].srf")]
public class PPSecureController(
    ILogger<PPSecureController> logger,
    IRazorTemplateEngine razorTemplateEngine,
    Authentication.AuthenticationClient authenticationClient,
    DeviceRegistration.DeviceRegistrationClient deviceRegistrationClient) : ControllerBase
{
    private const string WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private const string PpcrlNamespace =
        "http://schemas.microsoft.com/Passport/SoapServices/PPCRL";

    [HttpPost]
    [ActionName("DeviceAddCredential")]
    [EnableRateLimiting("DeviceRegisterLimit")]
    public async Task<IActionResult> DeviceAddCredential()
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync();

        var document = new XmlDocument { PreserveWhitespace = true };
        try
        {
            document.LoadXml(body);
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "RST2 request was not well-formed XML");
            return BadRequest();
        }

        var username = document.SelectSingleNode("//Membername")?.InnerText;
        var password = document.SelectSingleNode("//Password")?.InnerText;

        if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest();
        }

        var response = await authenticationClient.RegisterDeviceAsync(new RegisterDeviceRequest()
        {
            Username = username,
            Password = password,
            Requests = { }
        });

        var model = new DeviceAddResponseModel(response.Puid.ToString("X16"));
        var content = await razorTemplateEngine.RenderAsync("~/Views/DeviceAddCredential.success.cshtml", model);
        return Content(content, "application/soap+xml");
    }

    [HttpPost]
    [ActionName("DeviceChangeCredential")]
    [EnableRateLimiting("DeviceRegisterLimit")]
    public async Task<IActionResult> DeviceChangeCredential()
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync();

        var document = new XmlDocument { PreserveWhitespace = true };
        try
        {
            document.LoadXml(body);
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "RST2 request was not well-formed XML");
            return BadRequest();
        }

        var puid = document.SelectSingleNode("//Puid")?.InnerText;
        var password = document.SelectSingleNode("//Password")?.InnerText;
        var newPassword = document.SelectSingleNode("//NewPassword")?.InnerText;

        if (string.IsNullOrEmpty(puid) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest();
        }

        try
        {
            var response = await authenticationClient.ChangePasswordAsync(new ChangePasswordRequest()
            {
                Puid = puid,
                OldPassword = password,
                NewPassword = newPassword,
            });

            var model = new DeviceAddResponseModel(puid);
            var content = await razorTemplateEngine.RenderAsync("~/Views/DeviceChangeCredential.success.cshtml", model);
            return Content(content, "application/soap+xml");
        }
        catch (RpcException ex)
        {
            var content = await razorTemplateEngine.RenderAsync("~/Views/DeviceChangeCredential.fail.cshtml", new DeviceAddResponseModel("", "0x8004805F"));
            return Content(content, "application/soap+xml");
        }
    }

    // TODO(wam): we mint a fresh key each call, nothing persists it
    [HttpPost]
    [ActionName("GetUserKeyData")]
    [EnableRateLimiting("DeviceRegisterLimit")]
    public async Task<IActionResult> GetUserKeyData()
    {
        var document = await ReadSoapAsync();
        if (document is null)
            return BadRequest();

        var userCipher = ExtractDaCipher(document, "//wsse:Security/wsse:BinarySecurityToken");
        if (userCipher is null)
            return await FaultAsync("GetKeyData", PassportErrors.Unauthenticated);

        var auth = await authenticationClient.GetSecurityTokensAsync(
            new SecurityTokensRequest { DeviceAuthToken = userCipher });
        if (auth.Code != 0)
            return await FaultAsync("GetKeyData", auth.Code);

        var timestamp = PassportSoap.FormatZ(DateTimeOffset.UtcNow);
        var purposes = ExtractKeyPurposes(document)
            .Select(purpose => new GetKeyDataPurpose(
                purpose,
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                timestamp))
            .ToList();

        var model = new GetKeyDataModel(purposes);

        var content = await razorTemplateEngine.RenderAsync("~/Views/GetKeyData.success.cshtml", model);
        return Content(content, "application/soap+xml");
    }

    [HttpPost]
    [ActionName("DeviceAssociate")]
    [EnableRateLimiting("DeviceRegisterLimit")]
    public async Task<IActionResult> DeviceAssociate()
    {
        var document = await ReadSoapAsync();
        if (document is null)
            return BadRequest();

        var userCipher = ExtractDaCipher(document, "//wsse:Security/wsse:BinarySecurityToken");
        var deviceCipher = ExtractDaCipher(document, "//wsse:Embedded/wsse:BinarySecurityToken");
        if (userCipher is null || deviceCipher is null)
            return await FaultAsync("DeviceAssociate", PassportErrors.Unauthenticated);

        var user = await authenticationClient.GetSecurityTokensAsync(
            new SecurityTokensRequest { DeviceAuthToken = userCipher });
        if (user.Code != 0)
            return await FaultAsync("DeviceAssociate", user.Code);

        var device = await authenticationClient.GetSecurityTokensAsync(
            new SecurityTokensRequest { DeviceAuthToken = deviceCipher });
        if (device.Code != 0)
            return await FaultAsync("DeviceAssociate", device.Code);

        if (string.IsNullOrEmpty(device.DeviceId))
        {
            logger.LogWarning("DeviceAssociate: device token resolved to an account without a DeviceId (puid {Puid:X16})", device.Puid);
            return await FaultAsync("DeviceAssociate", PassportErrors.Unauthenticated);
        }

        var association = await deviceRegistrationClient.AssociateDeviceWithUserAsync(
            new DeviceAssociationRequest { DeviceId = device.DeviceId, UserId = user.Id });
        if (!association.Succeeded)
        {
            logger.LogWarning("DeviceAssociate: no device record for unique id {DeviceId}", device.DeviceId);
            return await FaultAsync("DeviceAssociate", PassportErrors.Unauthenticated);
        }

        var content = await razorTemplateEngine.RenderAsync("~/Views/DeviceAssociate.success.cshtml");
        return Content(content, "application/soap+xml");
    }

    private async Task<XmlDocument?> ReadSoapAsync()
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync();

        var document = new XmlDocument { PreserveWhitespace = true };
        try
        {
            document.LoadXml(body);
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "PPCRL request was not well-formed XML");
            return null;
        }

        return document;
    }

    private static List<string> ExtractKeyPurposes(XmlDocument document)
    {
        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("ps", PpcrlNamespace);

        var nodes = document.SelectNodes("//ps:GetKeyDataRequest/ps:KeyPurposes/ps:KeyPurpose", ns);
        var purposes = nodes?
            .Cast<XmlNode>()
            .Select(node => node.InnerText.Trim())
            .Where(purpose => purpose.Length > 0)
            .ToList();

        return purposes is { Count: > 0 } ? purposes : ["CredentialKey"];
    }

    private static string? ExtractDaCipher(XmlDocument document, string xpath)
    {
        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("wsse", WsseNamespace);

        var token = document.SelectSingleNode(xpath, ns)?.InnerText;
        return PassportSoap.CipherValueFromDaTokenWireForm(token);
    }

    private async Task<IActionResult> FaultAsync(string viewPrefix, uint code)
    {
        var model = new PpsecureFaultModel($"0x{code:X8}");
        var content = await razorTemplateEngine.RenderAsync($"~/Views/{viewPrefix}.fail.cshtml", model);
        return Content(content, "application/soap+xml");
    }
}
