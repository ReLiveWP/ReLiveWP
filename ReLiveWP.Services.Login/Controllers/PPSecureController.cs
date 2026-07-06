using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Razor.Templating.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models.DeviceCredential;

namespace ReLiveWP.Services.Login.Controllers;

[Consumes("application/soap+xml")]
[Produces("application/soap+xml")]
[Route("/ppsecure/{action}.srf")]
public class PPSecureController(
    ILogger<PPSecureController> logger,
    IRazorTemplateEngine razorTemplateEngine,
    Authentication.AuthenticationClient authenticationClient) : ControllerBase
{
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

        var model = new DeviceAddResponseModel(response.Puid.ToString("X2").PadLeft(16, '0'));
        var content = await razorTemplateEngine.RenderAsync("~/Views/DeviceAddCredential.success.cshtml", model);
        return Content(content, "application/soap+xml");
    }
}
