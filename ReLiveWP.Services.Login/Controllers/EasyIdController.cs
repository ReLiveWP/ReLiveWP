using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReLiveWP.Services.Login.Services;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Controllers;

public class EasyIdController(
    ILogger<EasyIdController> logger,
    GwpCertificateValidator certificateValidator) : ControllerBase
{
    [HttpPost("/services/easyId/create")]
    [EnableRateLimiting("AuthTokens")]
    public async Task<IActionResult> CreateAccount()
    {
        var certificateCheck = certificateValidator.CheckRequestCertificate(Request);
        var body = await DeviceRequestBody.ReadTrimmedBodyAsync(Request);
        var userData = EasyIdUserDataParser.ParseUserData(body);

        logger.LogInformation(
            "easyId create: cert {Subject} ({Thumbprint}) genuine {Genuine} {Reason}, LCID {Lcid}, IMSI {Imsi}, client-version {ClientVersion}, member {Member}, ski {Ski}, body {BodyLength} chars",
            certificateCheck.Certificate?.Subject,
            certificateCheck.Certificate?.Thumbprint,
            certificateCheck.IsGenuine,
            certificateCheck.FailureReason,
            Request.Headers["LCID"].ToString(),
            Request.Headers["IMSI"].ToString(),
            Request.Headers["client-version"].ToString(),
            userData?.MemberName,
            userData?.Ski,
            body.Length);

        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}
