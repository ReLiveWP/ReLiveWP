using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Marketplace.Controllers
{
    [Route("/{version}/{language}/apps/{id}/{action=Index}")]
    public class AppsController : Controller
    {
        public IActionResult Index(string id)
        {
            return File($"app_{id}.xml", "application/feed+xml");
        }

        public IActionResult Reviews(string id)
        {
            return File($"app_{id}.xml", "application/feed+xml");
        }

        public IActionResult PrimaryImage(string id)
        {
            return File($"app_{id}.png", "image/png");
        }
    }
}
