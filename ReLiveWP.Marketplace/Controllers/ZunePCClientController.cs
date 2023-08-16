using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Marketplace.Controllers
{
    [Route("/{language}/ZunePCClient/{version}/{action}.xml")]
    public class ZunePCClientController : Controller
    {
        public IActionResult Configuration()
        {
            // this returns a config file which should enable all zune features
            return File("configuration.xml", "application/atom+xml");
        }
    }
}
