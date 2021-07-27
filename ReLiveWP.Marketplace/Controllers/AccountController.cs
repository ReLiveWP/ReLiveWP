using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Marketplace.Data;
using ReLiveWP.Marketplace.Utilities;

namespace ReLiveWP.Marketplace.Controllers
{  
    [Route("/account/{action}")]
    [Route("/{version}/account/{action}")]
    [Route("/{version}/{language}/account/{action}")]
    public class AccountController : Controller
    {
        public IActionResult SignIn()
        {
            var id = "232AB414-7827-4A42-9FE3-6D8CE43E0450";

            var writer = new UTF8StringWriter();
            var resp = new SignInResponse();
            resp.AccountInfo.ZuneTag = "WamWooWam";
            resp.AccountInfo.Xuid = 123456789;
            resp.AccountInfo.UserReadID = new Guid(id);
            resp.AccountInfo.UserWriteID = new Guid(id);
            resp.AccountInfo.Locale = "en-GB";

            resp.SubscriptionInfo.BillingInstanceId = new Guid(id);

            var serialiser = new XmlSerializer(typeof(SignInResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(writer, resp);

            // this is a login token sent for other requests (like purchases)
            // asp.net core authrorization when:tm:
            Response.Cookies.Append("ZuneECommerce", "todo: generate commerse token");

            return Content(writer.ToString(), "application/xml");
        }

        public IActionResult Balances()
        {
            var str = new UTF8StringWriter();
            var resp = new BalanceResponse();
            var serialiser = new XmlSerializer(typeof(BalanceResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(str, resp);

            return Content(str.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
