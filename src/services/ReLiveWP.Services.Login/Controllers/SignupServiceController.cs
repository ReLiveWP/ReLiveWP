using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Razor.Templating.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models.Signup;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Controllers;

public class SignupServiceController(
    ILogger<SignupServiceController> logger,
    IRazorTemplateEngine razorTemplateEngine,
    Authentication.AuthenticationClient authenticationClient) : ControllerBase
{
    [HttpPost("/ws/SignupService.asmx")]
    [EnableRateLimiting("AuthTokens")]
    public async Task<IActionResult> InvokeSignupService()
    {
        var body = await DeviceRequestBody.ReadTrimmedBodyAsync(Request);
        var signinName = SignupServiceSoap.ReadSigninName(body);
        if (string.IsNullOrWhiteSpace(signinName))
        {
            logger.LogWarning("unsupported SignupService call, SOAPAction {Action}", Request.Headers["SOAPAction"].ToString());
            return BadRequest();
        }

        var availability = await authenticationClient.CheckUserNameAvailabilityAsync(
            new UserNameAvailabilityRequest() { Username = signinName });

        logger.LogInformation("signin name {Name} availability 0x{Code:X8}", signinName, availability.Code);

        var model = new SigninNameAvailabilityModel(availability.Code == 0);
        var content = await razorTemplateEngine.RenderAsync("~/Views/CheckSigninNameAvailability.cshtml", model);
        return Content(content, "text/xml; charset=utf-8");
    }
}
