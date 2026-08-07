using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Support.Models;
using ReLiveWP.Services.Support.Services;

namespace ReLiveWP.Services.Support.Controllers;

public class FwlinkController(
    KbContentStore articles,
    FwlinkStore links,
    IOptions<SupportOptions> options,
    ILogger<FwlinkController> logger) : Controller
{
    private readonly SupportOptions _options = options.Value;

    [HttpGet("/fwlink")]
    [HttpGet("/fwlink/p")]
    public IActionResult Forward()
    {
        var id = ReadLinkId();
        if (id is null)
        {
            logger.LogInformation("Forwarding request carried no LinkID");
            return RedirectToAction(nameof(Unknown));
        }

        return Forward(id.Value);
    }

    [HttpGet("/fwlink/{id:int}")]
    public IActionResult Forward(int id)
    {
        // an article claiming the id wins: front matter is the only place a KB link is written down
        if (articles.FindByFwlink(id) is { } article)
            return Forward(_options.Resolve(article.Url), passthrough: false);

        if (links.Find(id) is { } link)
        {
            var target = link.Target.StartsWith('/') ? _options.Resolve(link.Target) : link.Target;
            return Forward(target, link.Passthrough);
        }

        logger.LogInformation("Forwarding link {Id} is not allocated", id);
        return RedirectToAction(nameof(Unknown), new { id });
    }

    private IActionResult Forward(string target, bool passthrough)
    {
        if (passthrough && Request.QueryString.HasValue)
            target += (target.Contains('?') ? "&" : "?") + Request.QueryString.Value!.TrimStart('?');

        // a retargeted link has to take effect immediately, so never let this response be cached
        Response.Headers.CacheControl = "no-cache, no-store";

        return Redirect(target);
    }

    [HttpGet("/fwlink/notfound")]
    public IActionResult Unknown(int? id)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Unknown", id);
    }

    private int? ReadLinkId()
    {
        foreach (var (key, value) in Request.Query)
        {
            if (!string.Equals(key, "linkid", StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(value.FirstOrDefault(), out var id))
                return id;
        }

        return null;
    }
}
