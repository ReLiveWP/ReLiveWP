using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

// the desktop 8.1 account-switch wizard navigates here after FinalNext
public class SkyDriveSettingsController(ILogger<SkyDriveSettingsController> logger) : Controller
{
    [HttpGet]
    [HttpPost]
    [ActionName("SkyDriveSettings")]
    [Route("/windows/skydrivesettings")]
    public IActionResult SkyDriveSettings(SkyDriveSettingsContext context)
    {
        LogRequest();
        return View("SkyDriveSettings", context);
    }

    // TODO(wam): we do not know what this step is meant to collect. logging the whole request until
    // we do, then this can shrink to the bound model
    private void LogRequest()
    {
        var query = string.Join(", ", Request.Query.Select(q => $"{q.Key}={q.Value}"));
        var form = Request.HasFormContentType
            ? string.Join(", ", Request.Form.Select(f => $"{f.Key}={Truncate(f.Value.ToString())}"))
            : "(no form)";

        logger.LogInformation("skydrivesettings {Method} query [{Query}] form [{Form}]",
            Request.Method, query, form);
    }

    private static string Truncate(string value)
        => value.Length > 120 ? value[..120] + "..." : value;
}
