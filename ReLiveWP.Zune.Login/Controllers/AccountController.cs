﻿using System.Text;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Zune.Commerce.Models;

namespace ReLiveWP.Zune.Commerce.Controllers;


[Route("/account/{action}")]
[Route("/{version}/account/{action}")]
[Route("/{version}/{language}/account/{action}")]
public class AccountController : Controller
{
    [HttpPost]
    public async Task<SignInResponse> SignIn([FromBody] SignInRequest request)
    {
        //// do we know this is how this works?
        //var zuneId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TunerInfo.ID)));
        //var uid = "232AB414-7827-4A42-9FE3-6D8CE43E0450";

        //var resp = new SignInResponse();
        //resp.AccountInfo.ZuneTag = "WamWooWam";
        //resp.AccountInfo.Xuid = 123456789;
        //resp.AccountInfo.UserReadID = new Guid(uid);
        //resp.AccountInfo.UserWriteID = new Guid(uid);
        //resp.AccountInfo.Locale = "en-GB";

        //resp.SubscriptionInfo.BillingInstanceId = new Guid(uid);

        //// this is a login token sent for other requests (like purchases)
        //// asp.net core authrorization when:tm:
        //Response.Cookies.Append("ZuneECommerce", "todo: generate commerse token");

        //return resp;
    }

    public BalanceResponse Balances()
    {
        return new BalanceResponse();
    }
}
