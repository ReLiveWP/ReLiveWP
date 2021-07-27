using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Marketplace.Controllers
{
    [Route("/{version}/{language}/hubs/{action}")]
    [Route("/{version}/{language}/clientTypes/{clientType}/hubTypes/{action}/hub")]
    public class HubController : Controller
    {
        public IActionResult Apps(string version, string language, string clientType)
        {
            return File("appshub.xml", "application/atom+xml");
        }

        public IActionResult Games(string version, string language, string clientType)
        {
            return File("gameshub.xml", "application/atom+xml");
        }

        public IActionResult Marketplace(string version, string language, string clientType)
        {
            return File("marketplacehub.xml", "application/atom+xml");
        }
    }
}
