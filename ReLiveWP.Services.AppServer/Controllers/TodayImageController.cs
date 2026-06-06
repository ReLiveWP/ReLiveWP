using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.AppServer.Controllers;

[Area("BackgroundImage")]
public class TodayImageController : ControllerBase
{
    [ActionName("GetTodayImage")]
    public async Task<IActionResult> GetTodayImageAsync(
        [FromQuery] double dateOffset = 0, // represents -0
        [FromQuery] bool urlEncodeHeaders = true,
        [FromQuery] string osName = "wince",
        [FromQuery] string osVersion = "7.10",
        [FromQuery] string orientation = "480x800",
        [FromQuery] string deviceName = "DeviceName",
        [FromQuery(Name = "mkt")] string market = "en-GB",
        [FromQuery(Name = "AppId")] string? appId = null,
        [FromQuery(Name = "UserId")] string? userId = null)
    {
        var day = (DateTime.UtcNow + TimeSpan.FromHours(dateOffset)).Day;
        return LocalRedirect($"/today/windows_collection/{day}.jpg");
    }
}
