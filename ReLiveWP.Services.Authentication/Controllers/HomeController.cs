using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Authentication.Controllers;

[Route("/{action}.srf")]
public class HomeController : Controller
{
    public IActionResult PPCrlCheck()
    {
        return Content(@"<Config>
<DeviceID minversion=""7.0.13340.0""/>
<MobileCfg minversion=""7.0.13340.0""/>
</Config>");
    }
}
