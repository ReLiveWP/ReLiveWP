using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Zune.Catalog.Controllers;


[Route("/{version}/{language}/hubs/{action}")]
[Route("/{version}/{language}/clientTypes/{clientType}/hubTypes/{action}/hub")]
public class HubController : Controller
{
    public IActionResult Music(string version, string language, string clientType)
    {
        return File("musichub.xml", "application/atom+xml");
    }
}