using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Push.Controllers;

[ApiController]
[Route("/{controller}/{version}")]
public class BootstrapController(IConfiguration configuration, IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(string version)
    {
        var ip = configuration["Push:IP"];
        var port = int.Parse(configuration["Push:Port"]);
        if (ip == null)
        {
            using var client = httpClientFactory.CreateClient();
            ip = await client.GetStringAsync("https://api.ipify.org/");
        }

        var uri = new UriBuilder();
        uri.Scheme = "tcps";
        uri.Host = ip;
        uri.Port = port;

        return Content("Dip:" + uri.Uri.ToString());
    }
}
