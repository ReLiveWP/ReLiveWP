using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Zune.Commerce.Models;

namespace ReLiveWP.Zune.Commerce.Controllers;

[Route("/account/{action}")]
[Route("/{version}/account/{action}")]
[Route("/{version}/{language}/account/{action}")]
public class AccountController(User.UserClient userService) : Controller
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


        var uid = User.Id()!;
        var resp = new SignInResponse
        {
            AccountState = new AccountState(),
            AccountInfo = new AccountInfo()
            {
                ZuneTag = userInfo.Username,
                Xuid = (ulong)userInfo.Puid,
                UserReadID = new Guid(uid),
                UserWriteID = new Guid(uid),
                Locale = "en-GB",
            }
        };

        resp.SubscriptionInfo.BillingInstanceId = new Guid(uid);

        // this is a login token sent for other requests (like purchases)
        // asp.net core authrorization when:tm:
        Response.Cookies.Append("ZuneECommerce", value);

        return Ok(resp);
    }

    public BalanceResponse Balances()
    {
        return new BalanceResponse();
    }
}
