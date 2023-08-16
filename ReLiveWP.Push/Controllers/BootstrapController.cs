using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Push.Controllers
{
    [Route("/{controller}/{version}/{action=Index}")]
    public class BootstrapController : Controller
    {
        public IActionResult Index(string version)
        {
            return Content("Dip:tcp://10.0.0.233:2345");
        }
    }
}
