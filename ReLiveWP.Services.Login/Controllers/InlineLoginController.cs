using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Razor.Templating.Core;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

// serves the SPA used by for interactive login
public class InlineLoginController() : Controller
{
    [HttpGet]
    [Route("/ppsecure/InlineConnect.srf")]
    public async Task<IActionResult> InlineConnect(
        [FromQuery] string? id,
        [FromQuery] string? mkt,
        [FromQuery] string? lc,
        [FromQuery] string? opid,
        [FromQuery] string? uaid)
    {
        return LocalRedirectPermanent(Url.Action("InlineLogin", new { id, mkt, lc, opid, uaid })!);
    }

    [HttpGet]
    [ActionName("InlineLogin")]
    [Route("/ppsecure/InlineLogin.srf")]
    public async Task<IActionResult> InlineLogin(
        [FromQuery] string? id,
        [FromQuery] string? mkt,
        [FromQuery] string? lc,
        [FromQuery] string? opid,
        [FromQuery] string? uaid)
    {
        var model = new InlineLoginModel(
            Id: id ?? "",
            Mkt: mkt ?? "EN-US",
            Lc: lc ?? "",
            Opid: opid ?? "",
            Uaid: uaid ?? "",
            PostUrl: "/auth/inline_login",
            AssetBase: "/ppsecure/assets");

        return View("InlineLogin", model);
    }
}
