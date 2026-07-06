using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Login.Controllers;

[Route("/ppcrlcheck.srf")]
public class PPCrlCheckController : Controller
{
    public IActionResult Index()
    {
        return Content(@"<Config>
<DeviceID minversion=""7.0.13340.0""/>
<MobileCfg minversion=""7.0.13340.0""/>
</Config>");
    }
}
