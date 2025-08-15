using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Zune.Commerce.Models;

namespace ReLiveWP.Zune.Commerce.Controllers;

[Route("/account/{action}")]
[Route("/{version}/account/{action}")]
[Route("/{version}/{language}/account/{action}")]
public class AccountController(User.UserClient userService, Authentication.AuthenticationClient authService) : Controller
{
    [HttpPost]
    public async Task<ActionResult<SignInResponse>> SignIn([FromBody] SignInRequest request)
    {
        if (User == null)
            return NotFound();

        if (!HttpContext.Request.Headers.TryGetValue("Authorization", out var values) || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
            return NotFound();
        
        var value = values.FirstOrDefault()!;
        if (value.StartsWith("WLID1.0 "))
            value = value[8..];

        var userInfo = await userService.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });
        var tokenRequest = new SecurityTokensRequest() { AuthToken = value, Requests = { new SecurityTokenRequest() { ServiceTarget = "commerce.zune.net", ServicePolicy = "COOKIE" } } };
        var tokenResponse = await authService.GetSecurityTokensAsync(tokenRequest);

        // do we know this is how this works?
        var zuneId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TunerInfo.ID)));
        var uid = User.Id()!;

        var resp = new SignInResponse();
        resp.AccountInfo.ZuneTag = userInfo.Username;
        resp.AccountInfo.Xuid = userInfo.Puid;
        resp.AccountInfo.UserReadID = new Guid(uid);
        resp.AccountInfo.UserWriteID = new Guid(uid);
        resp.AccountInfo.Locale = "en-GB";

        resp.SubscriptionInfo.BillingInstanceId = new Guid(uid);

        // this is a login token sent for other requests (like purchases)
        // asp.net core authrorization when:tm:
        Response.Cookies.Append("ZuneECommerce", tokenResponse.Tokens.First().Token);

        return Ok(resp);
    }

    public BalanceResponse Balances()
    {
        return new BalanceResponse();
    }
}
