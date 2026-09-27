using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Login.Services;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Controllers;

public class SignupPublicKeyController(SignupKeyRing keyRing) : ControllerBase
{
    [HttpGet("/ppsecure/JSPublicKey.srf")]
    public IActionResult GetPublicKeyScript()
    {
        if (keyRing.CurrentKey is not { } key)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var script = SignupCrypto.FormatPublicKeyScript(key.Ski, key.Rsa);
        return Content(script, "text/javascript");
    }
}
