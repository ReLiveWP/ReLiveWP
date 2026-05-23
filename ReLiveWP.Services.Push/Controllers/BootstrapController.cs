using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Push.Controllers;

[Route("/{controller}/{version}/{action=Index}")]
public class BootstrapController : Controller
{
    public IActionResult Index(string version)
    {
        return Content("Dip:tcps://10.0.0.7:6969/");
    }
}
