using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Push.Controllers;

[Route("/{controller}/{version}/{action=Index}")]
public class BootstrapController : Controller
{
    public IActionResult Index(string version)
    {
        return Content("Dip:tcp://172.16.0.2:2345");
    }
}
