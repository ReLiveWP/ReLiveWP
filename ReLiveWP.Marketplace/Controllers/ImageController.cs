using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Marketplace.Controllers
{
    [Route("/{version}/{language}/image/{id}")]
    public class ImageController : Controller
    {
        public IActionResult Index(string id)
        {
            return File("deepfried.png", "image/png");
        }
    }
}
